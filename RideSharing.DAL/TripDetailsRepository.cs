using RideSharing.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.DAL
{
    public class TripDetailsRepository
    {
        private static string QueryString = "Data Source=.;Initial Catalog=RideSharing;Integrated Security=True";

        public long StoreTrips(List<TripDetails> TripDetails)
        {
            long processedRecords = 0;
            using (var connection = new SqlConnection(QueryString))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.BatchSize = 1000;
                    bulkCopy.DestinationTableName = "dbo.TripDetails";
                    try
                    {
                        bulkCopy.WriteToServer(TripDetails.AsDataTable());
                        processedRecords += 1000;
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();
                        return 0;
                    }
                }

                transaction.Commit();
            }
            return 1;
        }

        public void StoreSimulations(long SimulationId, string StartDate, string EndDate, int Poolsize, string ProcessingStartTime, string ProcessingEndTime)
        {
            using (var connection = new SqlConnection(QueryString))
            {
                connection.Open();

                string getRecords = "INSERT INTO SIMULATIONS values (" + SimulationId + ", '" + StartDate + "',  '" + EndDate + "', " + Poolsize + ", '" + ProcessingStartTime + "', '" + ProcessingEndTime + "' )";

                SqlCommand command = new SqlCommand(getRecords, connection);
                var returnValue = command.ExecuteNonQuery();              

                connection.Close();
            }
            
        }

        public List<long> GetSimulationIds()
        {
            List<long> returnData = new List<long>();

            using (var connection = new SqlConnection(QueryString))
            {
                connection.Open();

                string getRecords = "SELECT DISTINCT SimulationId FROM TripDetails";

                SqlCommand command = new SqlCommand(getRecords, connection);
                var returnValue = command.ExecuteReader();
                while (returnValue.Read())
                {
                    returnData.Add(Convert.ToInt64(returnValue[0]));
                }

                connection.Close();
            }

            return returnData;
        }

        public SimulationViewModel GetSimulationDetails(long Id)
        {
            SimulationViewModel returnData = new SimulationViewModel();

            using (var connection = new SqlConnection(QueryString))
            {
                connection.Open();

                string getRecords = " SELECT *, ((ST.TotalTrips - ST.TotalCabs) * 100)/ST.TotalTrips AS PercentageSaved FROM " +
                                    " (SELECT S.Id, S.PoolSize, COUNT(T.Id) AS TotalTrips, COUNT(DISTINCT T.CabId) AS TotalCabs, " +
                                    " S.PoolStartTime, S.PoolEndTime, " +
                                    " DATEDIFF(SECOND, S.SimulationStartTime, S.SimulationEndTime) AS ProcessingTime " +
                                    " FROM SIMULATIONS S " +
                                    " INNER JOIN TripDetails T ON S.Id = T.SimulationId " +
                                    " GROUP BY S.PoolSize, S.Id, S.PoolStartTime, S.PoolEndTime, S.SimulationStartTime, S.SimulationEndTime) ST " +
                                    " WHERE ST.Id = " + Id;
                SqlCommand command = new SqlCommand(getRecords, connection);
                var returnValue = command.ExecuteReader();
                while (returnValue.Read())
                {
                    var tableName = "RideDetails" + DateTime.Parse(returnValue[4].ToString()).Year + "" + DateTime.Parse(returnValue[4].ToString()).Month.ToString("00");
                    returnData.SimulationId = Convert.ToInt64(returnValue[0]);
                    returnData.PoolSize = Convert.ToInt32(returnValue[1]);
                    returnData.TotalTripsBefore = Convert.ToInt32(returnValue[2]);
                    returnData.TotalTripsAfter = Convert.ToInt32(returnValue[3]);
                    returnData.StartDate = DateTime.Parse(returnValue[4].ToString()).ToString("MM/dd/yyyy HH:mm:ss");
                    returnData.EndDate = DateTime.Parse(returnValue[5].ToString()).ToString("MM/dd/yyyy HH:mm:ss");
                    returnData.ProcessingTime = Convert.ToInt32(returnValue[6]);
                    returnData.PercentageSaved = Convert.ToInt32(returnValue[7]);
                    returnData.Trips = GetTrips(Id, tableName);
                }

                connection.Close();
            }

            return returnData;
        }

        private List<TripViewModel> GetTrips(long Id, string tableName)
        {
            List<TripViewModel> returnData = new List<TripViewModel>();

            using (var connection = new SqlConnection(QueryString))
            {
                connection.Open();

                string getRecords = " SELECT TD.CabId, TD.RideId, TD.DropoffDateTime, RD.DropoffDateTime, RD.PassengerCount, RD.WaitTime, RD.WalkTime " +
                                    " FROM TripDetails TD " +
                                    " INNER JOIN " + tableName + " RD ON TD.RideId = RD.Id WHERE SimulationId=" + Id + 
                                    " ORDER BY SimulationId, CabId";

                SqlCommand command = new SqlCommand(getRecords, connection);
                var returnValue = command.ExecuteReader();
                while (returnValue.Read())
                {
                    TripViewModel td = new TripViewModel();
                    td.CabId = Convert.ToInt32(returnValue[0]);
                    td.RideId = Convert.ToInt64(returnValue[1]);
                    td.DropoffTime = DateTime.Parse(returnValue[2].ToString()).ToString("MM/dd/yyyy HH:mm:ss");
                    td.ActualDropoffTime = DateTime.Parse(returnValue[3].ToString()).ToString("MM/dd/yyyy HH:mm:ss");
                    td.NumPassengers = Convert.ToInt32(returnValue[4]);
                    td.DelayTime = Convert.ToInt32(returnValue[5]);
                    td.WalkingTime = Convert.ToInt32(returnValue[6]);
                    returnData.Add(td);
                }

                connection.Close();
            }

            return returnData;
        }
    }
}
