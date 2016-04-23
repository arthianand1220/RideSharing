using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Models
{
    public class RideDetails
    {
        public long RideDetailsId { get; set; }

        public RideSharingPosition Destination { get; set; }

        public DateTime DropoffTime { get; set; }

        public int PassengerCount { get; set; }

        public int WaitTime { get; set; }

        public int WalkTime { get; set; }

        public RideDetails()
        {
            Destination = new RideSharingPosition();
        }
    }
}
