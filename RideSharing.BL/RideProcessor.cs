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
    public class RideProcessor : IRideProcessor
    {
        private RideDetailsRepository rdRepo;

        public RideProcessor(RideDetailsRepository RDRepo)
        {
            rdRepo = RDRepo;
        }

        public List<RideSharingPosition> GetRideLocations(string StartDate, string EndDate)
        {
            string tableName = GetTableName(StartDate);
            return rdRepo.GetRideSharingPositions(StartDate, EndDate, tableName);
        }

        public List<RideDetails> GetRides(string StartDate, string EndDate)
        {
            string tableName = GetTableName(StartDate);
            return new List<RideDetails>();
        }

        private string GetTableName(string startDate)
        {
            var date = DateTime.Parse(startDate);
            string tableName = "RideDetails" + date.Year + date.Month.ToString("00");
            return tableName;
        }
    }
}
