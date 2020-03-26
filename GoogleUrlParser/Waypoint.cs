using System;
using System.Collections.Generic;
using System.Text;

namespace GoogleUrlParser
{
    public class Waypoint
    {
        public double Latitude { get; internal set; }
        public double Longitude { get; internal set; }
        public string Address { get; internal set; } = "";
        public bool IsWaypoint { get; internal set; } = true;
        public bool IsViapoint { get; internal set; } = false;

        public Waypoint(bool isWaypointArg, double latitudeArg, double longitudeArg, string addrArg)
        {
            // Google precision is limited to 6 digits

            IsWaypoint = isWaypointArg;
            IsViapoint = !isWaypointArg;
            Latitude = Math.Round(latitudeArg * 1000000.0d) / 1000000.0d;
            Longitude = Math.Round(longitudeArg * 1000000.0d) / 1000000.0d;
            Address = addrArg;
        }

        public Waypoint(bool isWaypointArg, GeoPoint geoPt, string addrArg)
        {
            // Google precision is limited to 6 digits

            IsWaypoint = isWaypointArg;
            IsViapoint = !isWaypointArg;
            Latitude = Math.Round(geoPt.Latitude * 1000000.0d) / 1000000.0d;
            Longitude = Math.Round(geoPt.Longitude * 1000000.0d) / 1000000.0d;
            Address = addrArg;
        }

    }
}
