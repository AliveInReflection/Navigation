
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Navigation.Infrastructure
{
    public static class Helpers
    {
        public static bool IsUndefined(this object str)
        {
            return str.Equals("undefined");
        }
    }
}
