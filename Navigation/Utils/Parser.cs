using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Navigation.Models;

namespace Navigation.Infrastructure
{
    public static class Parser
    {
        public static Measurement Parse(string source)
        {
            var stringList = source.Split(',');

            var timeString = new StringBuilder(stringList[1]);
            timeString.Insert(2, ":");
            timeString.Insert(5, ":");

            var latitudeString = stringList[2].Insert(2, " ");
            var latitude = new Coordinate(Convert.ToDouble(latitudeString.Split().First()),
                                          Convert.ToDouble(latitudeString.Split().Last()));


            var longitudeString = stringList[4].Insert(3, " ");
            var longitude = new Coordinate(Convert.ToDouble(longitudeString.Split().First()),
                                          Convert.ToDouble(longitudeString.Split().Last()));
            
            var height = Convert.ToDouble(stringList[9]);
            var time = TimeSpan.Parse(timeString.ToString().Split('.').First());

            return new Measurement(latitude, longitude, height, time);
        }
    }
}
