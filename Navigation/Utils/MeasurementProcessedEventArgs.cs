using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Navigation.Models;

namespace Navigation.Utils
{
    public class MeasurementProcessedEventArgs : EventArgs
    {
        public MeasurementProcessedEventArgs(Measurement measurement)
        {
            Measurement = measurement;
        }

        public Measurement Measurement { get; private set; }
    }
}
