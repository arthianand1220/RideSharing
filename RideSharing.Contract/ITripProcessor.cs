using RideSharing.Models;
using System.Collections.Generic;

namespace RideSharing.Contract
{
    public interface ITripProcessor
    {
        bool RunSimulations(string startDate, string endDate, int numSimulations, int poolSize);

        bool ProcessTrips(string startDate, string endDate);

        List<TripDetails> GetTrips(long simulationId);
    }
}
