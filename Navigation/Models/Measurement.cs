using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Navigation.Infrastructure;

namespace Navigation.Models
{
    public class Measurement
    {
        public Measurement(Coordinate latitude, Coordinate longitude, double height, TimeSpan time)
        {
            Latitude = latitude;
            Longitude = longitude;
            Height = height;
            Time = time;

            CalculateXYZ();
            CalculateNomenclature();
        }

        public Coordinate Latitude { get; private set; }
        public Coordinate Longitude { get; private set; }
        public double Height { get; private set; }
        public TimeSpan Time { get; private set; }

        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        public double Distance { get; private set; }
        public double Path { get; private set; }
        public double FlightTime { get; private set; }
        public double Speed { get; private set; }
        public double Course { get; private set; }

        public string Nomenclature { get; private set; }

        public void CalculateMotionParameters(Measurement previous)
        {
            Distance = Math.Abs(1000 * Constants.R * Math.Acos(Math.Sin(previous.Latitude.Radians) * Math.Sin(Latitude.Radians)
                          + Math.Cos(previous.Latitude.Radians) * Math.Cos(Latitude.Radians)
                          * Math.Cos(previous.Longitude.Radians - Longitude.Radians)));

            Path = previous.Path + Distance;

            var duration = Time.TotalSeconds - previous.Time.TotalSeconds;

            FlightTime = previous.FlightTime + duration;

            Speed = Distance/duration;

            var course = Math.Atan((X - previous.X)/(Y - previous.Y)) * 180 / Math.PI;

            Course = course != Double.NaN ? course : 90;
        }

        private void CalculateXYZ()
        {
            double e2 = (Constants.a * Constants.a - Constants.b * Constants.b) / (Constants.a * Constants.a);

            double N = Constants.a / Math.Sqrt(1 - (e2 * Math.Sin(Latitude.Radians) * Math.Sin(Latitude.Radians)));

            X = (N + Height) * Math.Cos(Latitude.Radians) * Math.Cos(Longitude.Radians);
            Y = (N + Height) * Math.Cos(Latitude.Radians) * Math.Sin(Longitude.Radians);
            Z = ((1 - e2) * N + Height) * Math.Sin(Latitude.Radians);
        }

        private void CalculateNomenclature()
        {
            var row = (char)(Latitude.Degrees/4+1+64);
            var column = (int)(Longitude.Degrees/6+1+30);

            var sectorRow = (int)((((int)(Latitude.Degrees / 4) + 1) * 4 - Latitude.Degrees) * 60 - Latitude.Minutes) / 20;
            
            var sectorColumn = (int)(((Longitude.Degrees - (int)(Longitude.Degrees /6 ) * 6) * 60 + Longitude.Minutes) / 30 + 1);

            var sector = sectorRow * 12 + sectorColumn;

            if (sector < 100)
            {
                Nomenclature = string.Format("{0}-0{1}-{2}", row, column, sector);
            }
            else
            {
                Nomenclature = string.Format("{0}-{1}-{2}", row, column, sector);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}; {1}; {2}; {3}; {4}; {5}; {6}; {7};",
                Latitude, Longitude, Height, Time, X, Y, Z, Distance);
        }
    }
}
