using System.Collections.Generic;
using GMap.NET;

namespace Navigation.Models
{
    public class PathModel
    {
        public IEnumerable<IEnumerable<PointLatLng>> Track { get; set; }
        public decimal Distance { get; set; }
    }
}