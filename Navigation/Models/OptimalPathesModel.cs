using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMap.NET;

namespace Navigation.Models
{
    public class OptimalPathesModel
    {
        public OptimalPathModel First { get; set; }
        public OptimalPathModel Second { get; set; }
        public OptimalPathModel Third { get; set; }
    }

    public class OptimalPathModel
    {
        public IEnumerable<IEnumerable<PointLatLng>> Track { get; set; }
        public decimal Distance { get; set; }
    }
}
