using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using Navigation.Infrastructure;
using Navigation.Models;
using OxyPlot;
using INotifyPropertyChanged = GMap.NET.ObjectModel.INotifyPropertyChanged;

namespace Navigation.Utils
{
    public class MeasurementProcessor : IDisposable
    {
        private StreamReader _file;
        public List<Measurement> Measurements { get; private set; }
        public int BadMeasurementsCount { get; private set; }

        private double _averageSpeed;
        private double _minSpeed = double.MaxValue;
        private double _maxSpeed;
        private double _minHeight = double.MaxValue;
        private double _maxHeight;

        public event EventHandler<MeasurementProcessedEventArgs> MeasurementProcessed;
        public event EventHandler BadMeasurementProcessed;

        public MeasurementProcessor()
        {
            Measurements = new List<Measurement>();
            Finished = true;
        }

        public void Reinitialize(string filePath)
        {
            if (_file != null)
            {
                _file.Dispose();
            }
            Measurements.Clear();
            _file = new StreamReader(filePath);
            Finished = false;
        }

        public void ProcessNextMeasurement()
        {
            var measurementString = _file.ReadLine();

            if (measurementString != null)
            {
                if (measurementString.Contains("GPGGA"))
                {
                    measurementString = measurementString.Substring(measurementString.IndexOf("GPGGA"));
                    
                    try
                    {
                        var measurement = Parser.Parse(measurementString);
                        if (Measurements.Any())
                        {
                            measurement.CalculateMotionParameters(Measurements.Last());
                        }

                        Measurements.Add(measurement);

                        AverageSpeed = measurement.Speed;
                        MinSpeed = measurement.Speed;
                        MaxSpeed = measurement.Speed;
                        MinHeight = measurement.Height;
                        MaxHeight = measurement.Height;

                        OnMeasurementProcessed(measurement);
                    }
                    catch (ArgumentException)
                    {
                        BadMeasurementsCount++;
                        OnBadMeasurementProcessed();
                    }
                }
            }
            else
            {
                Finished = true;
            }
        }

        public void Reset()
        {
             _file.DiscardBufferedData();
             _file.BaseStream.Seek(0, SeekOrigin.Begin);
             Measurements.Clear();

            AverageSpeed = 0;
            MaxSpeed = 0;
            MaxHeight = 0;
            MinHeight = double.MaxValue;
            MinSpeed = double.MaxValue;

            BadMeasurementsCount = 0;
        }

        public bool Finished { get; private set; }

       
        public void Dispose()
        {
            if (_file != null)
            {
                _file.Dispose();
            }
        }

        private void OnMeasurementProcessed(Measurement measurement)
        {
            if (MeasurementProcessed != null)
            {
                MeasurementProcessed(this, new MeasurementProcessedEventArgs(measurement));
            }
        }

        private void OnBadMeasurementProcessed()
        {
            if (BadMeasurementProcessed != null)
            {
                BadMeasurementProcessed(this, new EventArgs());
            }
        }

        #region statistics
        public double AverageSpeed
        {
            get { return _averageSpeed; }
            private set { _averageSpeed = (_averageSpeed + value)/2; }
        }

        public double MinSpeed
        {
            get { return _minSpeed; }
            private set
            {
                if (value < _minSpeed && value != 0)
                {
                    _minSpeed = value;
                }
            }
        }

        public double MaxSpeed
        {
            get { return _maxSpeed; }
            private set
            {
                if (value > _maxSpeed)
                {
                    _maxSpeed = value;
                }
            }
        }

        public double MinHeight
        {
            get { return _minHeight; }
            private set
            {
                if (value < _minHeight)
                {
                    _minHeight = value;
                }
            }
        }

        public double MaxHeight {
            get { return _maxHeight; }
            private set
            {
                if (value > _maxHeight)
                {
                    _maxHeight = value;
                }
            }
        }
        #endregion
    }
}
