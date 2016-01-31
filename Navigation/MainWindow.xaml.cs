using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Navigation.Utils;
using OxyPlot;
using MessageBox = System.Windows.MessageBox;
using Timer = System.Timers.Timer;

namespace Navigation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MeasurementProcessor _processor;
        private Timer _timer;

        private PointLatLng _focus;
        private bool _ran;
        private bool _fileOpened;
        public int Zoom { get; set; }
        private GMapOverlay _trajectoryOverlay;

        public MainWindow()
        {
            _focus = new PointLatLng(50.022596, 36.2268269);
            _processor = new MeasurementProcessor();
            _timer = new Timer(30);

            DataContext = this;

            _ran = false;
            _fileOpened = false;

            InitializeComponent();
            InitializeMap();
            InitializeGraphs();
            
            _processor.MeasurementProcessed += OnMeasurementProcessed;
            _processor.BadMeasurementProcessed += OnBadMeasurementProcessed;
            _timer.Elapsed += TimerOnElapsed;

            this.Closing += MainWindow_Closing;
        }

        private void InitializeMap()
        {
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            GoogleMapProvider.Language = LanguageType.Russian;
            Map.MapProvider = GoogleMapProvider.Instance;

            Map.Position = _focus;
            Map.MinZoom = 1;
            Map.MaxZoom = 20;
            Map.Zoom = 12;
            ZoomSlider.Value = 12;
            Map.DragButton = MouseButtons.Left;
            Map.MapScaleInfoEnabled = true;

            Map.OnMapZoomChanged += OnMapWheelZooming;

            _trajectoryOverlay = new GMapOverlay("trajectory");
            Map.Overlays.Add(_trajectoryOverlay);

        }

        private void InitializeGraphs()
        {
            TrajectoryPoints.ItemsSource = new List<DataPoint>();
            SpeedPoints.ItemsSource = new List<DataPoint>();
            HeightPoints.ItemsSource = new List<DataPoint>();
            PathPoints.ItemsSource = new List<DataPoint>();
            CoursePoints.ItemsSource = new List<DataPoint>();
        }

       
        # region handlers

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_processor.Finished)
            {
                _timer.Stop();
                Dispatcher.Invoke(() =>
                {
                    StatusLabel.Content = "Finished...";
                });
            }

            _processor.ProcessNextMeasurement();
        }

        private void OnMeasurementProcessed(object sender, MeasurementProcessedEventArgs args)
        {
            var measurement = args.Measurement;
            try
            {
                Dispatcher.Invoke(() =>
                {
                    StatusLabel.Content = "On Measurement Processed...";

                    _trajectoryOverlay.Markers.Add(
                        new GMarkerGoogle(new PointLatLng(measurement.Latitude.AsDouble, measurement.Longitude.AsDouble),
                            GMarkerGoogleType.brown_small));
                    Map.Position = new PointLatLng(measurement.Latitude.AsDouble, measurement.Longitude.AsDouble);

                    (TrajectoryPoints.ItemsSource as List<DataPoint>).Add(new DataPoint(measurement.Longitude.AsDouble, measurement.Latitude.AsDouble));
                    (SpeedPoints.ItemsSource as List<DataPoint>).Add(new DataPoint(measurement.FlightTime, measurement.Speed));
                    (HeightPoints.ItemsSource as List<DataPoint>).Add(new DataPoint(measurement.FlightTime, measurement.Height));
                    (PathPoints.ItemsSource as List<DataPoint>).Add(new DataPoint(measurement.FlightTime, measurement.Path));
                    (CoursePoints.ItemsSource as List<DataPoint>).Add(new DataPoint(measurement.FlightTime, measurement.Course));

                    TrajectoryPlot.InvalidatePlot(true);
                    SpeedPlot.InvalidatePlot(true);
                    HeightPlot.InvalidatePlot(true);
                    PathPlot.InvalidatePlot(true);
                    CoursePlot.InvalidatePlot(true);

                    Table.Items.Add(new
                    {
                        Number = Table.Items.Count,
                        Latitude = measurement.Latitude.ToString(),
                        Longitude = measurement.Longitude.ToString(),
                        Height = measurement.Height,
                        Time = measurement.Time,
                        Speed = Math.Round(measurement.Speed, 2),
                        Path = Math.Round(measurement.Path, 2),
                        Course = measurement.Course,
                        Nomenclature = measurement.Nomenclature
                    });


                    TotalPathLabel.Content = string.Format("Total path: {0}m", Math.Round(measurement.Path, 2));
                    AverageSpeedLabel.Content = string.Format("Average speed: {0}m/s", Math.Round(_processor.AverageSpeed, 2));
                    MinSpeedLabel.Content = string.Format("Min speed: {0}m/s", Math.Round(_processor.MinSpeed, 2));
                    MaxSpeedLabel.Content = string.Format("Max speed: {0}m/s", Math.Round(_processor.MaxSpeed, 2));
                    MinHeightLabel.Content = string.Format("Min height: {0}m", Math.Round(_processor.MinHeight, 2));
                    MaxHeightLabel.Content = string.Format("Max height: {0}m", Math.Round(_processor.MaxHeight, 2));

                    var time = TimeSpan.FromSeconds(measurement.FlightTime);
                    FlightTimeLabel.Content = string.Format("Flight time: {0}h {1}m {2}s", time.Hours, time.Minutes, time.Seconds);
                });

            }
            catch (AggregateException e)
            {
                MessageBox.Show(e.InnerException.Message);
            }

        }

        private void OnBadMeasurementProcessed(object sender, EventArgs eventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                BadMeasurementsLabel.Content = string.Format("Bad measurements: {0}", _processor.BadMeasurementsCount);
            });
        }

        private void OnZoomSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var source = (Slider)sender;

            Map.Zoom = source.Value;
        }

        private void OnMapWheelZooming()
        {
            if (Map.Zoom < 2)
            {
                Map.Zoom = 2;
            }

            ZoomSlider.Value = Map.Zoom;
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "txt files (*.txt)|*.txt";
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                FilePathTextBox.Text = dialog.FileName;
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (FilePathTextBox.Text.Contains("."))
            {
                _processor.Reinitialize(FilePathTextBox.Text);
                StatusLabel.Content = "Ready to start...";
                _fileOpened = true;
            }
            else
            {
                MessageBox.Show("Incorrect file name");
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_ran && _fileOpened)
            {
                try
                {
                    _timer.Start();
                    _ran = true;
                    StatusLabel.Content = "Running...";

                }
                catch (AggregateException)
                {
                    MessageBox.Show("Please, open file with measurements before starting...");
                }
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ran)
            {
                _timer.Stop();
                _ran = false;
                StatusLabel.Content = "Paused...";
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_ran)
            {
                _timer.Stop();
            }
        }

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_ran)
            {
                _timer.Stop();
                _ran = false;
            }


            if (_fileOpened)
            {
                _processor.Reset();
                StatusLabel.Content = "Ready to start...";
            }

            _trajectoryOverlay.Markers.Clear();
            (TrajectoryPoints.ItemsSource as List<DataPoint>).Clear();
            (SpeedPoints.ItemsSource as List<DataPoint>).Clear();
            (HeightPoints.ItemsSource as List<DataPoint>).Clear();
            (PathPoints.ItemsSource as List<DataPoint>).Clear();
            (CoursePoints.ItemsSource as List<DataPoint>).Clear();

            Table.Items.Clear();

            TotalPathLabel.Content = string.Format("Total path: N/A");
            AverageSpeedLabel.Content = string.Format("Average speed: N/A");
            MinSpeedLabel.Content = string.Format("Min speed: N/A");
            MaxSpeedLabel.Content = string.Format("Max speed: N/A");
            MinHeightLabel.Content = string.Format("Min height: N/A");
            MaxHeightLabel.Content = string.Format("Max height: N/A");
        }


        #endregion

    }
}
