using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using GMap.NET;
using Navigation.Infrastructure;
using Navigation.Models;

namespace Navigation.Business
{
    public class Service
    {
        private object _lockTracks = new object();
        private object _lockResultTracks = new object();

        private readonly Repository _repository;

        public Service(Repository repository)
        {
            _repository = repository;
        }

        public async Task<OptimalPathesModel> GetOptimalPath(int idFrom, int idTo)
        {
            var connections = _repository.GetConnections();

            var distances = connections.SelectMany(c =>
            {
                var from = _repository.GetPoint(c.From);
                var to = _repository.GetPoint(c.To);
                decimal distance = (decimal)from.ToGeoCoordinate()
                                                .GetDistanceTo(to.ToGeoCoordinate());

                var distanceModel = new DistanceModel(from, to, distance);

                var ways = c.TwoWay ? new[] {distanceModel, distanceModel.Reverse()}
                                      : new[] { distanceModel };

                return ways;

            }).ToList();

            var endingIds = distances.Where(x => x.To.Id == idTo).Select(x => x.From.Id).ToList();

            var tracks = distances.Where(x => x.From.Id == idFrom).Select(x => new PathModel()
            {
                Points = new[] {x.From, x.To},
                Distance = x.Distance
            }).ToList();

            var resultTracks = tracks.Where(x => x.Points.Last().Id == idTo).ToList();
            resultTracks.ForEach(x => tracks.Remove(x));

            await Recurse(tracks, resultTracks, distances, endingIds, idTo);

            if (!resultTracks.Any())
            {
                throw new NoWayException();
            }

            var result = PrepareResult(resultTracks.OrderBy(x => x.Distance).Take(3).ToList());

            return result;
        }

        public Navigation.Models.PathModel ConvertToPathModel(IEnumerable<int> pointsIds)
        {
            var points = pointsIds.Select(id => _repository.GetPoint(id)).ToList();

            var distances = points
                .Where((t, i) => i < points.Count - 1)
                .Select((e, i) =>
                {
                    var connection = _repository.GetConnection(e.Id, points[i + 1].Id);

                    var from = _repository.GetPoint(e.Id);
                    var to = _repository.GetPoint(points[i + 1].Id);
                    decimal distance = (decimal)from.ToGeoCoordinate().GetDistanceTo(to.ToGeoCoordinate());

                    return new DistanceModel(from, to, distance);
                }).AsEnumerable()
                .ToList();

            var pathModel = new PathModel(distances);

            var result = new Models.PathModel()
            {
                Track = GetPoly(pathModel.Points),
                Distance = pathModel.Distance
            };

            return result;
        }

        private OptimalPathesModel PrepareResult(List<PathModel> pathes)
        {
            var result = new OptimalPathesModel();

            var path = pathes.FirstOrDefault();
            result.First = new Models.PathModel()
            {
                Track = GetPoly(path.Points),
                Distance = path.Distance
            };

            pathes.Remove(path);
            path = pathes.FirstOrDefault();

            if (path == null)
            {
                return result;
            }

            result.Second = new Models.PathModel()
            {
                Track = GetPoly(path.Points),
                Distance = path.Distance
            };

            pathes.Remove(path);
            path = pathes.FirstOrDefault();

            if (path == null)
            {
                return result;
            }

            result.Third = new Models.PathModel()
            {
                Track = GetPoly(path.Points),
                Distance = path.Distance
            };

            return result;
        }

        private IEnumerable<IEnumerable<PointLatLng>> GetPoly(IList<Point> pointsTrack)
        {
            var result = pointsTrack
                .Where((t, i) => i < pointsTrack.Count - 1)
                .Select((e, i) => new[] { e.ToMapPoint(), pointsTrack[i + 1].ToMapPoint() }.AsEnumerable())
                .ToList();

            return result;
        }

        private async Task Recurse(IList<PathModel> tracks, IList<PathModel> resultTracks, IList<DistanceModel> distances, IEnumerable<int> endingIds, int idTo)
        {
            if (!tracks.Any())
            {
                return;
            }

            var currentTracks = tracks.ToList();
            tracks.Clear();

            var tasks = currentTracks.Select(track =>
            {
                return Task.Factory.StartNew(() =>
                {

                    var edges = distances.Where(x => x.From.Id == track.Points.Last().Id &&
                                                     track.Points.All(y => x.To.Id != y.Id)).ToList();

                    edges.ForEach(p =>
                    {
                        var currentPath = new PathModel(track, p.To, p.Distance);
                        if (p.To.Id == idTo)
                        {
                            lock (_lockResultTracks)
                            {
                                resultTracks.Add(currentPath);
                                if (resultTracks.Count > 3)
                                {
                                    resultTracks.Remove(resultTracks.OrderByDescending(t => t.Distance).First());
                                }
                            }
                            return;
                        }

                        if (track.Points.Any(point => point.Id == p.To.Id))
                        {
                            return;
                        }

                        if (resultTracks.Count == 3)
                        {
                            lock (_lockResultTracks)
                            {
                                var longestPath = resultTracks.OrderByDescending(t => t.Distance).FirstOrDefault();
                                if (longestPath != null && currentPath.Distance >= longestPath.Distance)
                                {
                                    return;
                                }
                            }
                        }
                        lock (_lockTracks)
                        {
                            tracks.Add(currentPath);
                        }
                    });
                });
            });

            await Task.WhenAll(tasks);

            await Recurse(tracks, resultTracks, distances, endingIds, idTo);
        }

        private class DistanceModel
        {
            public DistanceModel(Point @from, Point to, decimal distance)
            {
                From = @from;
                To = to;
                Distance = distance;
            }

            public Point From { get; set; }
            public Point To { get; set; }
            public decimal Distance { get; set; }

            public DistanceModel Reverse()
            {
                return new DistanceModel(To, From, Distance);
            }
        }

        private class PathModel
        {
            public IList<Point> Points { get; set; }
            public decimal Distance { get; set; }

            public PathModel()
            {
                
            }

            public PathModel(PathModel prevPath, Point point, decimal distance)
            {
                Points = new List<Point>(prevPath.Points) {point};
                Distance = prevPath.Distance + distance;
            }

            public PathModel(IEnumerable<DistanceModel> distances)
            {
                Points = distances.Select(d => d.From).Union(distances.Select(d => d.To)).Distinct().ToList();
                Distance = distances.Sum(d => d.Distance);
            }
        }
    }
}
