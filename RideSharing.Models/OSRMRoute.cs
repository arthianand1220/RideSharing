using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Models
{
    public class Maneuver
    {
        public int bearing_after { get; set; }
        public List<double> location { get; set; }
        public int bearing_before { get; set; }
        public string type { get; set; }
        public int? exit { get; set; }
        public string modifier { get; set; }
    }

    public class Step
    {
        public string geometry { get; set; }
        public Maneuver maneuver { get; set; }
        public double duration { get; set; }
        public double distance { get; set; }
        public string name { get; set; }
        public string mode { get; set; }
    }

    public class Leg
    {
        public List<Step> steps { get; set; }
        public double duration { get; set; }
        public double distance { get; set; }
    }

    public class Trip
    {
        public List<Leg> legs { get; set; }
        public double duration { get; set; }
        public double distance { get; set; }
    }

    public class Route
    {
        public List<Leg> legs { get; set; }
        public double duration { get; set; }
        public double distance { get; set; }
    }

    public class Waypoint
    {
        public string hint { get; set; }
        public string name { get; set; }
        public List<double> location { get; set; }
    }

    public class OSRMRoute
    {
        public string code { get; set; }
        public List<Route> routes { get; set; }
        public List<Waypoint> waypoints { get; set; }
    }

    public class OSRMTrip
    {
        public string code { get; set; }
        public List<Trip> trips { get; set; }
        public List<Waypoint> waypoints { get; set; }
    }
}
