using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMap.NET.WindowsForms;
using Navigation.Models;

namespace Navigation.Map
{
    public class OwnPathOverlay : GMapOverlay
    {
        public Func<PathModel> GetPath;

        public Func<bool> ShowOwn;

        public void Update()
        {
            this.Polygons.Clear();

            if (GetPath() == null)
            {
                return;
            }

            foreach (var line in GetPath().Track)
            {
                var poly = new GMapPolygon(line.ToList(), GetPath().Distance.ToString());
                poly.Stroke = new Pen(Color.DarkViolet, 3f);
                this.Polygons.Add(poly);
                poly.IsVisible = ShowOwn();
            }

        }
    }
}
