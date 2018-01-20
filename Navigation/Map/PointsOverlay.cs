using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMap.NET.WindowsForms;
using Navigation.Business;
using Navigation.Infrastructure;

namespace Navigation.Map
{
    public class PointsOverlay : GMapOverlay
    {
        private readonly Repository _repository;

        public PointsOverlay(Repository repository)
        {
            _repository = repository;
        }

        public Func<GMapMarker> CurrentPoint;

        public void Update()
        {
            this.Markers.Clear();

            var markers = _repository.GetPoints().Select(p => p.ToMarker());
            foreach (var marker in markers)
            {
                this.Markers.Add(marker);
            }

            this.Markers.Add(CurrentPoint());
        }
    }
}
