using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using GMap.NET;
using GMap.NET.WindowsForms.Markers;

namespace Navigation.Models
{
    public class CustomMarker : GMarkerGoogle
    {
        public int Id { get; set; }
        public bool IsAirport { get; set; }
        public string Name { get; set; }
        
        public CustomMarker(PointLatLng p, int id, bool isAirport, string name) 
            : this(p, isAirport ? GMarkerGoogleType.red_small : GMarkerGoogleType.blue_small)
        {
            Id = id;
            IsAirport = isAirport;
            Name = name;

            ToolTipText = !String.IsNullOrEmpty(Name) ? name : id.ToString();
        }

        public CustomMarker(PointLatLng p, GMarkerGoogleType type) : base(p, type)
        {
        }

        public CustomMarker(PointLatLng p, Bitmap bitmap) : base(p, bitmap)
        {
        }

        public CustomMarker(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
