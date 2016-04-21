using RideSharing.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RideSharing.Models;
using RideSharing.DAL;

namespace RideSharing.BL
{
    public class TripProcessor : ITripProcessor
    {
        private RideDetailsRepository rdRepo;

        public TripProcessor(RideDetailsRepository RDRepo)
        {
            rdRepo = RDRepo;
        }

        public List<RideSharingPosition> GetRecordsWithinTimeFrame(string StartDate, string EndDate)
        {
            var date = DateTime.Parse(StartDate);
            string tableName = "RideDetails" + date.Year + date.Month.ToString("00");
            return rdRepo.GetRecords(StartDate, EndDate, tableName);
        }
    }
}
