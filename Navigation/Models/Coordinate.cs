using System;
using Navigation.Infrastructure;

namespace Navigation.Models
{
    public class Coordinate
    {
        public Coordinate(double degrees, double minutes)
        {
            Degrees = degrees;
            Minutes = minutes;
        }

        public double Degrees { get; set; }
        public double Minutes { get; set; }

        public double Radians
        {
            get
            {
                return (Degrees + Minutes / Constants.SecondsInMinute) * Math.PI / Constants.PiDegrees;
            }
        }

        public double AsDouble
        {
            get { return Degrees + Minutes/60; }
        }

        public override string ToString()
        {
            return string.Format("{0}° {1}'", Degrees, Minutes);
        }
    }
}
