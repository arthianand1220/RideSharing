using Microsoft.SqlServer.Types;
using RideSharing.Contract;
using RideSharing.DAL;
using RideSharing.Models;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RideSharing.BL
{
    public class DataProcessor : IDataProcessor
    {
        private RideDetailsRepository rdRepo;

        public DataProcessor(RideDetailsRepository RDRepo)
        {
            SqlServerTypes.Utilities.LoadNativeAssemblies(AppDomain.CurrentDomain.BaseDirectory);
            rdRepo = RDRepo;
        }

        public long ProcessData(string FilePath, int MaxWaitingTime, int MaxWalkingTime)
        {
            ConcurrentBag<RideDetails> tempRideDetails = new ConcurrentBag<RideDetails>();
            Random random = new Random();
            string TableName = Path.GetFileNameWithoutExtension(FilePath);

            Parallel.ForEach(File.ReadLines(FilePath), (line, _, lineNumber) =>
            {
                var tempLines = line.Split(',');
                try
                {
                    RideDetails dm = new RideDetails();
                    dm.Id = Convert.ToInt64(lineNumber);
                    var Latitude = Convert.ToDouble(tempLines[13]);
                    var Longitude = Convert.ToDouble(tempLines[12]);

                    if ((Longitude >= -73.825722 && Latitude >= 40.642354) &&
                                        (Longitude <= -73.752251 && Latitude <= 40.67491))
                    {
                        dm.Destination = SqlGeography.Point(Latitude, Longitude, 4326);
                        dm.PickupDateTime = DateTime.ParseExact(tempLines[5], "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        dm.DropoffDateTime = DateTime.ParseExact(tempLines[6], "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        dm.PassengerCount = Convert.ToInt32(tempLines[7]);
                        dm.Duration = Convert.ToDouble(tempLines[8]);
                        dm.Distance = Convert.ToDouble(tempLines[9]);
                        dm.WaitTime = random.Next(0, MaxWaitingTime);
                        dm.WalkTime = random.Next(0, MaxWalkingTime);
                        tempRideDetails.Add(dm);
                    }
                }
                catch (Exception)
                {
                }
            });

            return rdRepo.StoreRideDetails(tempRideDetails.OrderBy(o => o.PickupDateTime).ToList(), TableName);
        }
    }
}
