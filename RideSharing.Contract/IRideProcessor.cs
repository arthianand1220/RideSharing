using RideSharing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Contract
{
    public interface IRideProcessor
    {
        List<RideSharingPosition> GetRideLocations(string StartDate, string EndDate);

        List<RideDetails> GetRides(string StartDate, string EndDate);

        List<RideDetails> GetRidesByIds(List<long> Ids);
    }
}
