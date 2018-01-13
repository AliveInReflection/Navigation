using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using Navigation.Business;
using Navigation.Infrastructure;
using Navigation.Models;

namespace Navigation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string Undefined = "undefined";
        private const string Ready = "Ready";
        private PointLatLng _focus;
        private GMapMarker _currentPoint;
        private CustomMarker _selectedMarker;
        private OptimalPathesModel _optimalPathes;
        private List<int> _ownPath;

        public int Zoom { get; set; }
        private GMapOverlay _pointsOverlay;
        private GMapOverlay _connectionsOverlay;
        private GMapOverlay _minPathOverlay;
        private GMapOverlay _ownPathOverlay;

        private CustomMarker _selectedFrom;
        private CustomMarker _selectedTo;

        private readonly Repository _repository;
        private readonly Service _service;

        public MainWindow()
        {
            _ownPath = new List<int>();
            _repository = new Repository();
            _repository.Load();

            _service = new Service(_repository);


            DataContext = this;

            InitializeComponent();
            InitializeMap();
            
            this.Closing += MainWindow_Closing;
        }

        private void InitializeMap()
        {
            GMaps.Instance.Mode = AccessMode.ServerOnly;
            GoogleMapProvider.Language = LanguageType.Russian;
            Map.MapProvider = GoogleMapProvider.Instance;

            _focus = new PointLatLng(48.994636, 31.442871);
            _currentPoint = new GMarkerCross(_focus);
            Map.Position = _focus;
            Map.MinZoom = 1;
            Map.MaxZoom = 20;
            Map.Zoom = 5;
            ZoomSlider.Value = 5;
            Map.DragButton = MouseButtons.Left;
            Map.MapScaleInfoEnabled = true;

            Map.OnMapZoomChanged += OnMapWheelZooming;

            Map.OnMarkerClick += MapOnOnMarkerClick;
            Map.MouseClick += MapOnMouseClick;

            _pointsOverlay = new GMapOverlay();
            _connectionsOverlay = new GMapOverlay();
            _minPathOverlay = new GMapOverlay();
            _ownPathOverlay = new GMapOverlay();

            Map.Overlays.Add(_pointsOverlay);
            Map.Overlays.Add(_connectionsOverlay);
            Map.Overlays.Add(_minPathOverlay);
            Map.Overlays.Add(_ownPathOverlay);


            UpdatePointsOverlay();
            UpdateConnectionsOverlay();
        }

        # region handlers

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

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ClearPathFields();
            ClearConnectionFields();
            ClearCurrentPointFields();
            ClearMinPathesLength();
            ClearOwnPath();
            OwnPathCheckbox.IsChecked = false;

            CurrentLatLabel.Content = Undefined;
            CurrentLngLabel.Content = Undefined;

            _optimalPathes = null;

            UpdateMinPathsOverlay();

            SetStatus(Ready);
        }

        private void ClearMinPathesLength()
        {
            GreenLength.Content = "...";
            YellowLength.Content = "...";
            RedLength.Content = "...";
        }

        private void MapOnMouseClick(object sender, MouseEventArgs args)
        {
            if (args.Button == MouseButtons.Left)
            {
                double lat = Map.FromLocalToLatLng(args.X, args.Y).Lat;
                double lng = Map.FromLocalToLatLng(args.X, args.Y).Lng;

                CurrentLatLabel.Content = lat.ToString();
                CurrentLngLabel.Content = lng.ToString();

                _currentPoint.Position = new PointLatLng(lat, lng);
            }
        }

        private void MapOnOnMarkerClick(GMapMarker item, MouseEventArgs mouseEventArgs)
        {
            var markerInfo = (CustomMarker)item;

            _selectedMarker = markerInfo;

            if (OwnPathCheckbox.IsChecked == true)
            {
                DrawOwnPath();
                return;
            }

            UpdatePointId.Content = _selectedMarker.Id;
            UpdatePointName.Text = _selectedMarker.Name;
            IsAirpoirtUpdateCheckbox.IsChecked = _selectedMarker.IsAirport;

            if (_selectedFrom == null)
            {
                _selectedFrom = markerInfo;
                NewConnectionFromLabel.Content = 
                PathFromLabel.Content = String.IsNullOrEmpty(markerInfo.Name)
                    ? markerInfo.Id.ToString()
                    : markerInfo.Name;
            }
            else
            {
                _selectedTo = markerInfo;
                NewConnectionToLabel.Content =
                PathToLabel.Content = String.IsNullOrEmpty(markerInfo.Name)
                    ? markerInfo.Id.ToString()
                    : markerInfo.Name;
            }
        }

        private void AddPoint_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentLatLabel.Content.IsUndefined() || CurrentLngLabel.Content.IsUndefined())
            {
                SetStatus("Please, select location");
                return;
            }

            var point = new Models.Point()
            {
                Lat = double.Parse(CurrentLatLabel.Content as string),
                Lng = double.Parse(CurrentLngLabel.Content as string),
                IsAirport = IsAirpoirtCheckbox.IsChecked.Value,
                Name = CurrentPointName.Text
            };

            _repository.Add(point);
            _repository.Save();

            UpdatePointsOverlay();
            UpdateConnectionsOverlay();
            ClearConnectionFields();

            SetStatus(Ready);
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearConnectionFields();
            ClearPathFields();
            ClearMinPathesLength();
            _optimalPathes = null;
            UpdateMinPathsOverlay();


            SetStatus(Ready);
        }

        private void AddConnection_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFrom == null || _selectedTo == null)
            {
                SetStatus("Please select 2 markers to add new connection...");
                return;
            }

            _repository.Add(new Connection()
            {
                From = _selectedFrom.Id,
                To = _selectedTo.Id
            });
            
            _repository.Save();

            ClearConnectionFields();
            UpdateConnectionsOverlay();

            SetStatus(Ready);
        }

        private void DeleteConnection_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFrom == null || _selectedTo == null)
            {
                SetStatus("Both markers should be selected");
                return;
            }

            try
            {
                _repository.DeleteConnection(_selectedFrom.Id, _selectedTo.Id);
                _repository.Save();
            }
            catch (Exception exception)
            {
                SetStatus("Connection does not exist");
            }

            ClearConnectionFields();
            UpdateConnectionsOverlay();

            SetStatus(Ready);
        }

        private void DeletePoint_Click(object sender, RoutedEventArgs e)
        {
            if (UpdatePointId.Content.IsUndefined())
            {
                SetStatus("Please select marker first");
                return;
            }

            _repository.DeletePoint(_selectedMarker.Id);
            _repository.Save();

            ClearCurrentPointFields();
            UpdateOverlays();

            SetStatus(Ready);
        }

        private void UpdatePoint_Click(object sender, RoutedEventArgs e)
        {
            if (UpdatePointId.Content.IsUndefined())
            {
                SetStatus("Please select marker first");
                return;
            }

            _repository.UpdatePoint(_selectedMarker.Id, IsAirpoirtUpdateCheckbox.IsChecked.Value, UpdatePointName.Text);
            _repository.Save();

            ClearCurrentPointFields();
            UpdateOverlays();

            SetStatus(Ready);
        }

        private async void ShowPath_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFrom == null || 
                _selectedTo == null || 
                !_selectedFrom.IsAirport || 
                !_selectedTo.IsAirport)
            {
                SetStatus("Airports should be selected for path optimal path calculation");
                return;
            }

            var from = _selectedFrom.Id;
            var to = _selectedTo.Id;

            var task = Task.Factory.StartNew(async () =>
            {
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus("Searching...");
                        ShowPathButton.IsEnabled = false;
                    });

                    _optimalPathes = await _service.GetOptimalPath(from, to);

                    Dispatcher.Invoke(() =>
                    {
                        UpdateMinPathsOverlay();
                        UpdateMinPathesLength();
                        ClearPathFields();
                        SetStatus("Ready");
                        ShowPathButton.IsEnabled = true;

                    });
                }
                catch (NoWayException)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SetStatus("There is no way between these airports");
                        ClearPathFields();
                        _optimalPathes = null;
                        UpdateMinPathsOverlay();
                        ShowPathButton.IsEnabled = true;
                    });
                }

            });
        }

        private void UpdateMinPathesLength()
        {
            GreenLength.Content = (_optimalPathes.First.Distance / 1000).ToString("0.000");
            YellowLength.Content = (_optimalPathes.Second.Distance / 1000).ToString("0.000");
            RedLength.Content = (_optimalPathes.Third.Distance / 1000).ToString("0.000");
        }

        #endregion

        private void UpdatePointsOverlay()
        {
            _pointsOverlay.Markers.Clear();

            var markers = _repository.GetPoints().Select(p => p.ToMarker());
            foreach (var marker in markers)
            {
                _pointsOverlay.Markers.Add(marker);
            }

            _pointsOverlay.Markers.Add(_currentPoint);
        }

        private void UpdateConnectionsOverlay()
        {
            var connections = _repository.GetConnections();
            var points = _repository.GetPoints();

            var lines = connections.Select(c => points.Where(p => p.Id == c.From || p.Id == c.To)
                                                      .Select(p => p.ToMapPoint())
                                                      .ToList());

            _connectionsOverlay.Polygons.Clear();

            foreach (var line in lines)
            {
                var poly = new GMapPolygon(line, "Connection");
                poly.Stroke = new Pen(Color.CornflowerBlue, 1);
                //_connectionsOverlay.Polygons.Add(poly);
                _connectionsOverlay.Routes.Add(new GMapRoute(line, "")
                {
                    Stroke = new Pen(Color.CornflowerBlue, 2f)
                });
            }
        }

        private void UpdateMinPathsOverlay()
        {
            _minPathOverlay?.Polygons.Clear();

            if (_optimalPathes == null)
            {
                return;
            }

            if (_optimalPathes.Third != null)
            {
                foreach (var line in _optimalPathes.Third.Track)
                {
                    var poly = new GMapPolygon(line.ToList(), _optimalPathes.Third.Distance.ToString());
                    poly.Stroke = new Pen(Color.Red, 3f);
                    _minPathOverlay.Polygons.Add(poly);
                    poly.IsVisible = RedPathCheckbox.IsChecked.Value;
                }
            }

            if (_optimalPathes.Second != null)
            {
                foreach (var line in _optimalPathes.Second.Track)
                {
                    var poly = new GMapPolygon(line.ToList(), _optimalPathes.Second.Distance.ToString());
                    poly.Stroke = new Pen(Color.Yellow, 3f);
                    _minPathOverlay.Polygons.Add(poly);
                    poly.IsVisible = YellowPathCheckbox.IsChecked.Value;
                }
            }

            if (_optimalPathes.First != null)
            {
                foreach (var line in _optimalPathes.First.Track)
                {
                    var poly = new GMapPolygon(line.ToList(), _optimalPathes.First.Distance.ToString());
                    poly.Stroke = new Pen(Color.DarkGreen, 3f);
                    _minPathOverlay.Polygons.Add(poly);
                    poly.IsVisible = GreenPathCheckbox.IsChecked.Value;

                }
            }
        }

        private void UpdateOwnPathOverlay(OptimalPathModel ownPathModel)
        {
            foreach (var line in ownPathModel.Track)
            {
                var poly = new GMapPolygon(line.ToList(), ownPathModel.Distance.ToString());
                poly.Stroke = new Pen(Color.DarkViolet, 3f);
                _ownPathOverlay.Polygons.Add(poly);
                poly.IsVisible = RedPathCheckbox.IsChecked.Value;
            }
        }

        private void UpdateOverlays()
        {
            UpdatePointsOverlay();
            UpdateConnectionsOverlay();
        }

        private void ClearConnectionFields()
        {
            _selectedFrom = null;
            _selectedTo = null;

            NewConnectionFromLabel.Content = Undefined;
            NewConnectionToLabel.Content = Undefined;

            PathFromLabel.Content = Undefined;
            PathToLabel.Content = Undefined;
        }

        private void ClearCurrentPointFields()
        {
            UpdatePointId.Content = Undefined;
            UpdatePointName.Text = "";
            IsAirpoirtUpdateCheckbox.IsChecked = false;
        }

        private void SetStatus(string message)
        {
            StatusLabel.Content = message;
        }

        private void ClearPathFields()
        {
            _selectedFrom = null;
            _selectedTo = null;

            PathFromLabel.Content = Undefined;
            PathToLabel.Content = Undefined;
        }

        private void OptimalPath_OnChecked(object sender, RoutedEventArgs e)
        {
            UpdateMinPathsOverlay();
        }

        private void ClearOwnPath_Click(object sender, RoutedEventArgs e)
        {
            ClearOwnPath();
        }

        private void ClearOwnPath()
        {
            _ownPath.Clear();
            _ownPathOverlay.Clear();
            OwnPathLength.Content = "...";
        }
        private void DrawOwnPath()
        {
            if (_ownPath.Any() == false)
            {
                if (_selectedMarker.IsAirport == false)
                {
                    SetStatus("Path should start from airport");
                    return;
                }

                _ownPath.Add(_selectedMarker.Id);
                return;
            }

            _ownPath.Add(_selectedMarker.Id);

            try
            {
                var pathModel = _service.ConvertToPathModel(_ownPath);
                UpdateOwnPathOverlay(pathModel);
                OwnPathLength.Content = (pathModel.Distance / 1000).ToString("0.000");
                SetStatus("Ready");

            }
            catch (NoWayException e)
            {
                _ownPath.Remove(_ownPath.Last());
                SetStatus("There is no way between selected points");
            }
        }


    }
}
