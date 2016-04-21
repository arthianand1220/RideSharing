using Microsoft.SqlServer.Types;
using RideSharing.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;

namespace RideSharing.DAL
{
    public class RideDetailsRepository
    {
        public long StoreRideDetails(List<RideDetails> RideDetails, string TableName)
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

        public List<RideSharingPosition> GetRecords(string StartDate, string EndDate, string TableName)
        {
            List<RideSharingPosition> rideDetails = new List<RideSharingPosition>();

            using (var connection = new SqlConnection("Data Source=.;Initial Catalog=RideSharing;Integrated Security=True"))
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
                    rideDetails.Add(position);
                }
            }

            return rideDetails;
        }
    }
}
