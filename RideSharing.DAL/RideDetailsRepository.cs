using RideSharing.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace RideSharing.DAL
{
    public class RideDetailsRepository
    {
        private static string QueryString = "Data Source=.;Initial Catalog=RideSharing;Integrated Security=True";

        public long StoreRideDetails(List<RideDetailsDBRecord> RideDetails, string TableName)
        {
            long processedRecords = 0;
            using (var connection = new SqlConnection("Data Source=.;Initial Catalog=RideSharing;Integrated Security=True"))
            {
                connection.Open();
                SqlTransaction transaction = connection.BeginTransaction();
                int returnValue = 0;

                string createTable = " CREATE TABLE dbo." + TableName + "( " +
                                     " [Id][bigint] IDENTITY(1, 1) NOT NULL, " +
                                     " [PickupDateTime] [datetime] NOT NULL, " +
                                     " [DropoffDateTime] [datetime] NOT NULL, " +
                                     " [Destination] [geography] NOT NULL, " +
                                     " [Distance] [float] NOT NULL, " +
                                     " [Duration] [float] NOT NULL, " +
                                     " [PassengerCount] [smallint] NOT NULL, " +
                                     " [WaitTime] [int] NOT NULL, " +
                                     " [WalkTime] [int] NOT NULL " +
                                     " ) ON[PRIMARY] TEXTIMAGE_ON[PRIMARY]";
                using (SqlCommand command = new SqlCommand(createTable, connection, transaction))
                {
                    returnValue = command.ExecuteNonQuery();
                }

                if (returnValue == -1)
                {
                    using (var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                    {
                        bulkCopy.BatchSize = 1000;
                        bulkCopy.DestinationTableName = "dbo." + TableName;
                        try
                        {
                            bulkCopy.WriteToServer(RideDetails.AsDataTable());
                            processedRecords += 1000;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            connection.Close();
                        }
                    }
                }

                transaction.Commit();
            }
            return processedRecords;
        }

        public List<RideSharingPosition> GetRideSharingPositions(string StartDate, string EndDate, string TableName)
        {
            List<RideSharingPosition> returnData = new List<RideSharingPosition>();

            using (var connection = new SqlConnection(QueryString))
            {
                connection.Open();

                string getRecords = " SELECT Destination.Lat, Destination.Long FROM " + TableName +
                                    " WHERE PickupDateTime >= '" + StartDate + "' " + 
                                    " AND PickupDateTime <= '" + EndDate + "'";

                SqlCommand command = new SqlCommand(getRecords, connection);
                var returnValue = command.ExecuteReader();
                while(returnValue.Read())
                {
                    RideSharingPosition position = new RideSharingPosition();
                    position.Latitude = Convert.ToDouble(returnValue[0]);
                    position.Longitude = Convert.ToDouble(returnValue[1]);
                    returnData.Add(position);
                }

                connection.Close();
            }

            return returnData;
        }

        public List<RideDetails> GetRideDetails(string StartDate, string EndDate, string TableName)
        {
            List<RideDetails> returnData = new List<RideDetails>();

            using (var connection = new SqlConnection(QueryString))
            {
                connection.Open();

                string getRecords = " SELECT Id, (DropoffDateTime - (PickupDateTime - '" + StartDate + "')) As DropOffTime, " +
                                    " Destination.Lat, Destination.Long, PassengerCount, WaitTime, WalkTime, Distance, Duration" +
                                    " FROM " + TableName +
                                    " WHERE PickupDateTime >= '" + StartDate + "' " +
                                    " AND PickupDateTime <= '" + EndDate + "' AND PassengerCount <= 4";
                SqlCommand command = new SqlCommand(getRecords, connection);
                var returnValue = command.ExecuteReader();
                while(returnValue.Read())
                {
                    RideDetails rideDetails = new RideDetails();
                    rideDetails.RideDetailsId = Convert.ToInt64(returnValue[0]);
                    rideDetails.DropoffTime = Convert.ToDateTime(returnValue[1]);
                    rideDetails.Destination.Latitude = Convert.ToDouble(returnValue[2]);
                    rideDetails.Destination.Longitude = Convert.ToDouble(returnValue[3]);
                    rideDetails.PassengerCount = Convert.ToInt32(returnValue[4]);
                    rideDetails.WaitTime = Convert.ToInt32(returnValue[5]);
                    rideDetails.WalkTime = Convert.ToInt32(returnValue[6]);
                    rideDetails.ActualDistanceTravelled = Convert.ToDouble(returnValue[7]);
                    rideDetails.ActualDurationTravelled = Convert.ToDouble(returnValue[8]) / 60;
                    returnData.Add(rideDetails);
                }

                connection.Close();
            }
            return returnData;
        }
    }
}
