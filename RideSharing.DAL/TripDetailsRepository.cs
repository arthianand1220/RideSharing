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
                    catch (Exception ex)
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

        public List<TripDetails> GetSimulations(long Id)
        {
            List<TripDetails> returnData = new List<TripDetails>();

            using (var connection = new SqlConnection(QueryString))
            {
                connection.Open();

                string getRecords = "SELECT * FROM TripDetails WHERE SimulationId = " + Id;

                SqlCommand command = new SqlCommand(getRecords, connection);
                var returnValue = command.ExecuteReader();
                while (returnValue.Read())
                {
                    TripDetails td = new TripDetails();
                    td.Id = Convert.ToInt64(returnValue[0]);
                    td.SimulationId = Id;
                    td.CabId = Convert.ToInt32(returnValue[2]);
                    td.RideId = Convert.ToInt64(returnValue[3]);
                    td.PassengerCount = Convert.ToInt32(returnValue[4]);
                    td.SequenceNum = Convert.ToInt32(returnValue[5]);
                    td.PickupDateTime = DateTime.Parse(returnValue[7].ToString());
                    td.DropoffDateTime = DateTime.Parse(returnValue[8].ToString());
                    td.ActualDropoffDateTime = DateTime.Parse(returnValue[9].ToString());
                    td.DelayTime = Convert.ToInt32(returnValue[10]);
                    td.WalkTime = Convert.ToInt32(returnValue[11]);
                    returnData.Add(td);
                }

                connection.Close();
            }

            return returnData;
        }
    }
}
