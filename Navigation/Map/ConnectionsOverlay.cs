using System;
using System.Drawing;
using System.Linq;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Navigation.Business;
using Navigation.Infrastructure;

namespace Navigation.Map
{
    public class ConnectionsOverlay : GMapOverlay
    {
        private readonly Repository _repository;
        private GMapMarker _tooltip;
        public Action<string> ShowDirection;
        public Action HideDirrection;

        public ConnectionsOverlay(Repository repository)
        {
            _repository = repository;
        }

        public void Update()
        {
            var connections = _repository.GetConnections();
            var points = _repository.GetPoints();

            var lines = connections.Select(c => new
            {
                Points = points.Where(p => p.Id == c.From || p.Id == c.To).ToList(),
                TwoWay = c.TwoWay
            }).ToList();

            this.Polygons.Clear();

            foreach (var line in lines)
            {
                var from = line.Points.First();
                var to = line.Points.Last();

                this.Routes.Add(new CustomRoute(line.Points.Select(p => p.ToMapPoint()).ToList(), "")
                {
                    Stroke = line.TwoWay ? new Pen(Color.CornflowerBlue, 3f) : new Pen(Color.IndianRed, 3f),
                    TextFrom = String.IsNullOrEmpty(from.Name) ? from.Id.ToString() : from.Name,
                    TextTo = String.IsNullOrEmpty(to.Name) ? to.Id.ToString() : to.Name,
                    TwoWay = line.TwoWay
                });
            }
        }

        public void MapOnRouteLeave(GMapRoute item)
        {
            HideDirrection?.Invoke();
        }

        public void MapOnRouteEnter(GMapRoute item)
        {
            var route = (CustomRoute)item;
            var text = route.TwoWay ? "Two way route" : $"From {route.TextFrom} to {route.TextTo}";
            ShowDirection(text);
        }
    }
}