using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Navigation.Infrastructure
{
    public static class Constants
    {
        public const double R = 6371;
        public const double a = 6378137;
        public const double b = 6356752.314245;
        public const double PiDegrees = 180;
        public const double SecondsInMinute = 60;
        public const string Preffix = "GPGGA";
    }
}
