using Microsoft.SqlServer.Types;
using System;

namespace RideSharing.Models
{
    public class RideDetails
    {
        public float Id { get; set; }

        public DateTime PickupDateTime { get; set; }

        public DateTime DropoffDateTime { get; set; }

        public SqlGeography Destination { get; set; }

        public Double Distance { get; set; }

        public Double Duration { get; set; }

        public int PassengerCount { get; set; }

        public int WaitTime { get; set; }

        public int WalkTime { get; set; }
    }
}
