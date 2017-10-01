using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMap.NET;
using Navigation.Infrastructure;
using Navigation.Models;

namespace Navigation.Business
{
    public class Service
    {
        private const Decimal Inf = 10000000000000;
        private readonly Repository _repository;

        public Service(Repository repository)
        {
            _repository = repository;
        }

        public IEnumerable<IEnumerable<PointLatLng>> GetOptimalPath(int idFrom, int idTo)
        {
            var allPoints = _repository.GetPoints();
            var connections = _repository.GetConnections();

            var nodes = allPoints.Select((p, i) => new KeyValuePair<int, Point>(i, p))
                .ToDictionary(x => x.Key, x => x.Value);

            var distances = connections.Select(c =>
            {
                var from = _repository.GetPoint(c.From).ToGeoCoordinate();
                var to = _repository.GetPoint(c.To).ToGeoCoordinate();
                decimal distance = (decimal)from.GetDistanceTo(to);

                return new DistanceModel()
                {
                    From = nodes.First(x => x.Value.Id == c.From).Key,
                    To = nodes.First(x => x.Value.Id == c.To).Key,
                    Distance = distance
                };
            }).ToList();

            var size = nodes.Count;
            var graph = InitGraph(size, distances);

            bool[] used = new bool[size];
            decimal[] path = Enumerable.Range(0, size).Select(_ => Inf).ToArray();
            int[] travel = new int[size];

            var startIndex = nodes.First(x => x.Value.Id == idFrom).Key;
            var endIndex = nodes.First(x => x.Value.Id == idTo).Key;

            path[startIndex] = 0;
            used[startIndex] = true;

            FindMinPath(graph, path, used, travel, startIndex, endIndex);

            if (path[endIndex] >= Inf)
            {
                throw new NoWayException();
            }

            var result = RestoreTrack(nodes, travel, startIndex, endIndex);

            return result;
        }

        private static List<IEnumerable<PointLatLng>> RestoreTrack(Dictionary<int, Point> nodes, int[] travel, int startIndex, int endIndex)
        {
            var track = new List<int>();
            int index = endIndex;
            track.Insert(0, index);

            while (index != startIndex)
            {
                track.Insert(0, travel[index]);
                index = travel[index];
            }

            var pointsTrack = track.Select(x => nodes[x].ToMapPoint()).ToList();

            var result = pointsTrack.Where((t, i) => i < pointsTrack.Count - 1)
                .Select((e, i) => new[] { e, pointsTrack[i + 1] }.AsEnumerable())
                .ToList();

            return result;
        }

        private void FindMinPath(decimal[,] graph, decimal[] path, bool[] used, int[] travel, int startIndex, int endIndex)
        {
            var size = path.Length;
            var currentIndex = startIndex;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var currentLength = path[currentIndex] + graph[currentIndex, j];
                    if (graph[currentIndex, j] != 0 &&
                        currentLength < path[j] &&
                        !used[j])
                    {
                        path[j] = currentLength;
                        travel[j] = currentIndex;
                    }
                }

                used[currentIndex] = true;

                if (currentIndex == endIndex)
                {
                    break;
                }

                try
                {
                    currentIndex = FindIndexOmMinPath(path, used);
                }
                catch
                {
                    break;
                }
            }
        }

        private int FindIndexOmMinPath(decimal[] path, bool[] used)
        {
            var min = path.Where((x, i) => !used[i]).Min();

            var index = path.Select((x, i) => new KeyValuePair<int, decimal>(i, x)).First(x => x.Value == min).Key;

            return index;
        }

        private decimal[,] InitGraph(int size, IEnumerable<DistanceModel> distances)
        {
            var graph = new decimal[size, size];

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (i == j)
                    {
                        graph[i, j] = 0;
                    }
                    else
                    {
                        graph[i, j] = Inf;
                    }
                }
            }

            foreach (var distance in distances)
            {
                graph[distance.From, distance.To] = distance.Distance;
                graph[distance.To, distance.From] = distance.Distance;
            }

            return graph;
        }

        private class DistanceModel
        {
            public int From { get; set; }
            public int To { get; set; }
            public decimal Distance { get; set; }
        }
    }
}
