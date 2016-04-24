using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Models
{
    public class Destination
    {
        public string hint { get; set; }

        public string name { get; set; }

        public List<double> location { get; set; }
    }

    public class Source
    {
        public string hint { get; set; }

        public string name { get; set; }

        public List<double> location { get; set; }
    }

    public class OSRMDistanceMatrix
    {
        public string code { get; set; }

        public List<List<double>> durations { get; set; }

        public List<Destination> destinations { get; set; }

        public List<Source> sources { get; set; }
    }
}
