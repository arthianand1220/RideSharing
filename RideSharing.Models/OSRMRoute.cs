using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Models
{
    public class Leg
    {
        public List<object> steps { get; set; }
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

}
