using System;
using System.Collections.Generic;

namespace RideSharing.Models
{
    public class SimulationViewModel
    {
        public long SimulationId { get; set; }

        public long TotalTripsBefore { get; set; }

        public long TotalTripsAfter { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        public int ProcessingTime { get; set; }

        public int PoolSize { get; set; }

        public int PercentageSaved { get; set; }

        public List<TripViewModel> Trips { get; set; }

        public SimulationViewModel()
        {
            Trips = new List<TripViewModel>();
        }
    }
}