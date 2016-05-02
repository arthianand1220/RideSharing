using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Models
{
    public class TripViewModel
    {
        public int CabId { get; set; }

        public long RideId { get; set; }

        public string DropoffTime { get; set; }

        public string ActualDropoffTime { get; set; }

        public int NumPassengers { get; set; }

        public int DelayTime { get; set; }

        public int WalkingTime { get; set; }
    }
}
