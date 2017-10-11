using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GMap.NET;
using Navigation.Infrastructure;
using Navigation.Models;

namespace Navigation.Business
{
    public class Service
    {
        private readonly Repository _repository;

        public Service(Repository repository)
        {
            _repository = repository;
        }

        public OptimalPathesModel GetOptimalPath(int idFrom, int idTo)
        {
            var connections = _repository.GetConnections();

            var distances = connections.Select(c =>
            {
                var from = _repository.GetPoint(c.From);
                var to = _repository.GetPoint(c.To);
                decimal distance = (decimal)from.ToGeoCoordinate()
                                                .GetDistanceTo(to.ToGeoCoordinate());

                return new DistanceModel()
                {
                    From = from,
                    To = to,
                    Distance = distance
                };

            }).ToList();

            foreach (var distance in distances.ToList())
            {
                distances.Add(distance.Reverse());
            }

            var endingIds = distances.Where(x => x.To.Id == idTo).Select(x => x.From.Id).ToList();

            var tracks = distances.Where(x => x.From.Id == idFrom).Select(x => new PathModel()
            {
                Points = new[] {x.From, x.To},
                Distance = x.Distance
            }).ToList();

            var resultTracks = tracks.Where(x => x.Points.Last().Id == idTo).ToList();
            resultTracks.ForEach(x => tracks.Remove(x));

            Recurse(tracks, resultTracks, distances, endingIds, idTo);


            if (!resultTracks.Any())
            {
                throw new NoWayException();
            }

            var result = PrepareResult(resultTracks.OrderBy(x => x.Distance).Take(3).ToList());

            return result;
        }

        private OptimalPathesModel PrepareResult(List<PathModel> pathes)
        {
            var result = new OptimalPathesModel();

            var path = pathes.FirstOrDefault();
            result.First = new OptimalPathModel()
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

            result.Second = new OptimalPathModel()
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

            result.Third = new OptimalPathModel()
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

        private void Recurse(IList<PathModel> tracks, IList<PathModel> resultTracks, IList<DistanceModel> distances, IEnumerable<int> endingIds, int idTo)
        {
            if (!tracks.Any())
            {
                return;
            }

            var currentTracks = tracks.ToList();
            tracks.Clear();

            foreach (var track in currentTracks)
            {
                var edges = distances.Where(x => x.From.Id == track.Points.Last().Id && track.Points.All(y => x.To.Id != y.Id)).ToList();
                

                edges.ForEach(p =>
                {
                    var currentPath = new PathModel(track, p.To, p.Distance);
                    if (p.To.Id == idTo)
                    {
                        resultTracks.Add(currentPath);
                        if (resultTracks.Count > 3)
                        {
                            resultTracks.Remove(resultTracks.OrderByDescending(t => t.Distance).First());
                        }
                        return;
                    }

                    if (track.Points.Any(point => point.Id == p.To.Id))
                    {
                        return;
                    }

                    if (resultTracks.Count == 3)
                    {
                        var longestPath = resultTracks.OrderByDescending(t => t.Distance).First();
                        if (currentPath.Distance >= longestPath.Distance)
                        {
                            return;
                        }
                    }
                    tracks.Add(currentPath);
                });
            }

            Recurse(tracks, resultTracks, distances, endingIds, idTo);
        }

        private class DistanceModel
        {
            public Point From { get; set; }
            public Point To { get; set; }
            public decimal Distance { get; set; }

            public DistanceModel Reverse()
            {
                return new DistanceModel()
                {
                    From = To,
                    To = From,
                    Distance = Distance
                };
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
        }
    }
}
