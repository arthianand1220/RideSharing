using RideSharing.Models;
using System.Collections.Generic;

namespace RideSharing.Contract
{
    public interface ITripProcessor
    {
        bool RunSimulations(string StartDate, string EndDate, int NumSimulations, int PoolSize);

        bool ProcessTrips(long SimulationId, string StartDate, string EndDate);

        List<long> GetSimulationIds();

        SimulationViewModel GetTrips(long SimulationId);
    }
}
