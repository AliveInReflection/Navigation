using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Navigation.Models;

namespace Navigation.Business
{
    public class Repository
    {
        private StorageModel _data;

        private const string FilePath = "data.xml";

        public void Add(Point point)
        {
            ValidateData();

            if (_data.Points.Any(x => x.Lat == point.Lat && x.Lng == point.Lng))
            {
                return;
            }

            var id = _data.Points.Select(p => p.Id).OrderBy(_ => _).Last() + 1;
            point.Id = id;

            var list = _data.Points.ToList();
            list.Add(point);

            _data.Points = list.ToArray();
        }

       
        public void Add(Connection connection)
        {
            ValidateData();

            if (_data.Connections.Any(x => x.From == connection.From && x.To == connection.To))
            {
                return;
            }

            var list = _data.Connections.ToList();
            list.Add(connection);

            _data.Connections = list.ToArray();
        }

        public IEnumerable<Point> GetPoints()
        {
            ValidateData();

            return _data.Points;
        }

        public Point GetPoint(int id)
        {
            ValidateData();

            return _data.Points.First(x => x.Id == id);
        }

        public void UpdatePoint(int id, bool isAirport, string name)
        {
            ValidateData();

            var list = _data.Points.ToList();
            var existingPoint = list.First(x => x.Id == id);

            existingPoint.IsAirport = isAirport;
            existingPoint.Name = name;

            _data.Points = list.ToArray();
        }

        public void DeletePoint(int id)
        {
            ValidateData();

            var existingPoint = _data.Points.First(x => x.Id == id);
            var list = _data.Points.ToList();
            list.Remove(existingPoint);
            _data.Points = list.ToArray();

            DeleteConnectionswithPoint(id);
        }

        public IEnumerable<Connection> GetConnections()
        {
            ValidateData();

            return _data.Connections;
        }

        public void DeleteConnection(int fromId, int toId)
        {
            ValidateData();

            var existingConnection = _data.Connections.FirstOrDefault(x => x.From == fromId && x.To == toId) ?? 
                                     _data.Connections.First(x => x.From == toId && x.To == fromId);

            var list = _data.Connections.ToList();
            list.Remove(existingConnection);

            _data.Connections = list.ToArray();
        }

        public void Load()
        {
            if (!File.Exists(FilePath))
            {
                Initialize();
                return;
            }

            using (var stream = new FileStream(FilePath, FileMode.OpenOrCreate))
            {
                var serializer = new XmlSerializer(typeof(StorageModel));

                try
                {
                    _data = (StorageModel)serializer.Deserialize(stream);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Could not deserialize data file");
                }
            }

        }

        public void Save()
        {
            using (var stream = new FileStream(FilePath, FileMode.Truncate))
            {
                var serializer = new XmlSerializer(typeof(StorageModel));

                try
                {
                    serializer.Serialize(stream, _data);
                }
                catch (Exception e)
                {
                    throw new ArgumentException("Could not serialize data file");
                }
            }
        }

        private void Initialize()
        {
            _data = new StorageModel()
            {
                Points = new Point[] {},
                Connections = new Connection[]{ }
            };

            Save();
        }

        private void ValidateData()
        {
            if (_data == null)
            {
                throw new InvalidOperationException("Data is not loaded");
            }
        }

        private void DeleteConnectionswithPoint(int pointId)
        {
            var connections = _data.Connections.Where(x => x.From == pointId || x.To == pointId);
            var list = _data.Connections.ToList();

            foreach (var connection in connections)
            {
                list.Remove(connection);
            }

            _data.Connections = list.ToArray();
        }
    }
}
