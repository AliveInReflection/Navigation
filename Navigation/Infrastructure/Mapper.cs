using System.Device.Location;
using GMap.NET;
using Navigation.Models;

namespace Navigation.Infrastructure
{
    public static class Mapper
    {
        public static CustomMarker ToMarker(this Point point)
        {
            return new CustomMarker(new PointLatLng(point.Lat, point.Lng), 
                                    point.Id, 
                                    point.IsAirport, 
                                    point.Name);
        }

        public static PointLatLng ToMapPoint(this Point point)
        {
            return new PointLatLng(point.Lat, point.Lng);
        }

        public static GeoCoordinate ToGeoCoordinate(this Point point)
        {
            return new GeoCoordinate(point.Lat, point.Lng);
        }
    }
}
