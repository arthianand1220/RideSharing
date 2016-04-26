using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Models
{
    public class RideDetails : ICloneable
    {
        public long RideDetailsId { get; set; }

        public RideSharingPosition Destination { get; set; }

        public DateTime DropoffTime { get; set; }

        public double PreviousDistanceTravelled { get; set; }

        public double PreviousDurationTravelled { get; set; }

        public SortedDictionary<int, double> TimeMatrix { get; set; }

        public int PassengerCount { get; set; }

        public int WaitTime { get; set; }

        public int WalkTime { get; set; }

        public double CurrentDistanceTravelled { get; set; }

        public double CurrentDurationTravelled { get; set; }

        public RideDetails()
        {
            Destination = new RideSharingPosition();
            TimeMatrix = new SortedDictionary<int, double>();
            DropoffTime = DropoffTime.AddMinutes(WaitTime);
            CurrentDistanceTravelled = 0;
            CurrentDurationTravelled = 0;
        }

        public object Clone()
        {
            RideDetails ride = new RideDetails();
            ride.RideDetailsId = RideDetailsId;
            ride.Destination = Destination;
            ride.DropoffTime = DropoffTime;
            ride.TimeMatrix = TimeMatrix;
            ride.PassengerCount = PassengerCount;
            ride.WaitTime = WaitTime;
            ride.WalkTime = WalkTime;
            ride.CurrentDistanceTravelled = CurrentDistanceTravelled;
            ride.CurrentDurationTravelled = CurrentDurationTravelled;
            ride.PreviousDistanceTravelled = PreviousDistanceTravelled;
            ride.PreviousDurationTravelled = PreviousDurationTravelled;
            return ride;            
        }
    }
}
