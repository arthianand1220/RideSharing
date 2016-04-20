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
                int returnValue = -1;

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
                using (SqlCommand command = new SqlCommand(createTable, connection))
                returnValue = command.ExecuteNonQuery();

                if(returnValue > 0)
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
    }
}
