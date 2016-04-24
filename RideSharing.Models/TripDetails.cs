using Microsoft.SqlServer.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Models
{
    public class TripDetails
    {
        public long Id { get; set; }

        public long SimulationId { get; set; }

        public int CabId { get; set; }

        public long RideId { get; set; }

        public int PassengerCount { get; set; }

        public SqlGeography Destination { get; set; }

        public DateTime PickupTime { get; set; }

        public DateTime DropoffTime { get; set; }

        public int DelayTime { get; set; }

        public int WalkTime { get; set; }
    }
}
