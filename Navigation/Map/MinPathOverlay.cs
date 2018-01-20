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
    public class MinPathOverlay : GMapOverlay
    {
        public Func<bool> EnableRed;
        public Func<bool> EnableYellow;
        public Func<bool> EnableGreen;
        public Func<OptimalPathesModel> OptimalPathes;
             
        public void Update()
        {
            this.Polygons.Clear();

            if (OptimalPathes() == null)
            {
                return;
            }

            if (OptimalPathes()?.Third != null)
            {
                foreach (var line in OptimalPathes().Third.Track)
                {
                    var poly = new GMapPolygon(line.ToList(), OptimalPathes().Third.Distance.ToString())
                    {
                        Stroke = new Pen(Color.Red, 3f),
                        IsVisible = EnableRed?.Invoke() ?? false
                    };
                    this.Polygons.Add(poly);
                }
            }

            if (OptimalPathes()?.Second != null)
            {
                foreach (var line in OptimalPathes().Second.Track)
                {
                    var poly = new GMapPolygon(line.ToList(), OptimalPathes().Second.Distance.ToString())
                    {
                        Stroke = new Pen(Color.Yellow, 3f),
                        IsVisible = EnableYellow?.Invoke() ?? false
                    };
                    this.Polygons.Add(poly);
                }
            }

            if (OptimalPathes()?.First != null)
            {
                foreach (var line in OptimalPathes().First.Track)
                {
                    var poly = new GMapPolygon(line.ToList(), OptimalPathes().First.Distance.ToString())
                    {
                        Stroke = new Pen(Color.DarkGreen, 3f),
                        IsVisible = EnableGreen?.Invoke() ?? false
                    };
                    this.Polygons.Add(poly);

                }
            }

        }
    }
}
