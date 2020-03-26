using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleUrlParser
{
    public class GeoPoint
    {
        public double Latitude { get; internal set; }
        public double Longitude { get; internal set; }

        public GeoPoint(double latitudeArg, double longitudeArg)
        {
            // Google precision is limited to 6 digits

            Latitude = Math.Round(latitudeArg * 1000000.0d) / 1000000.0d;
            Longitude = Math.Round(longitudeArg * 1000000.0d) / 1000000.0d;
        }

        public string ToFormattedString()
        {
            return "Lat : " + Latitude.ToString("##.######") + " Lon : " + Longitude.ToString("###.######");
        }
    }
}
