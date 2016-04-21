using RideSharing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Contract
{
    public interface ITripProcessor
    {
        List<RideSharingPosition> GetRecordsWithinTimeFrame(string StartDate, string EndDate);
    }
}
