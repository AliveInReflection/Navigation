using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using GMap.NET;
using GMap.NET.WindowsForms;

namespace Navigation.Map
{
    public class CustomRoute : GMapRoute
    {
        public CustomRoute(string name) : base(name)
        {
            Init();
        }

        public CustomRoute(IEnumerable<PointLatLng> points, string name) : base(points, name)
        {
            Init();
        }

        public CustomRoute(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Init();
        }

        public string TextFrom { get; set; }

        public string TextTo { get; set; }

        public bool TwoWay { get; set; }

        private void Init()
        {
            IsVisible = true;
            IsHitTestVisible = true;
        }
    }
}
