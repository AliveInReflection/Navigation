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

            var lines = connections.Select(c => points.Where(p => p.Id == c.From || p.Id == c.To)
                                                .ToList());

            this.Polygons.Clear();

            foreach (var line in lines)
            {
                var from = line.First();
                var to = line.Last();

                this.Routes.Add(new CustomRoute(line.Select(p => p.ToMapPoint()).ToList(), "")
                {
                    Stroke = new Pen(Color.CornflowerBlue, 2f),
                    TextFrom = String.IsNullOrEmpty(from.Name) ? from.Id.ToString() : from.Name,
                    TextTo = String.IsNullOrEmpty(to.Name) ? to.Id.ToString() : to.Name
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
            var text = $"From {route.TextFrom} to {route.TextTo}";
            ShowDirection(text);
        }
    }
}