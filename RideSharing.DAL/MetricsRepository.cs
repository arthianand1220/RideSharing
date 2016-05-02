using RideSharing.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.DAL
{
    public class MetricsRepository
    {
        private static string QueryString = "Data Source=.;Initial Catalog=RideSharing;Integrated Security=True";

        public string GetProcessingTimeVSPoolSize()
        {
            string returnData = "";
            returnData += "PoolSize,Max Time, Avg Time, Min Tim\n";

            using (var connection = new SqlConnection(QueryString))
            {
                connection.Open();

                string getRecords = " SELECT S.PoolSize, " +
                                    " MAX(DATEDIFF(SECOND, S.SimulationStartTime, S.SimulationEndTime)) AS 'MaxProcessingTime', " +
                                    " AVG(DATEDIFF(SECOND, S.SimulationStartTime, S.SimulationEndTime)) AS 'AvgProcessingTime',  " +
                                    " MIN(DATEDIFF(SECOND, S.SimulationStartTime, S.SimulationEndTime)) AS 'MinProcessingTime'  " +
                                    " FROM " +
                                    " (SELECT COUNT(ID) AS Requests, SimulationId FROM TripDetails " +
                                    " GROUP BY SimulationId) TD " +
                                    " INNER JOIN Simulations S ON S.Id = TD.SimulationId " +
                                    " GROUP BY S.PoolSize";

                SqlCommand command = new SqlCommand(getRecords, connection);
                var returnValue = command.ExecuteReader();
                while (returnValue.Read())
                {
                    returnData += returnValue[0] + "," + returnValue[1] + "," + returnValue[2] + "," + returnValue[3] + "\n";
                }

                connection.Close();
            }

            return returnData;
        }

        public List<KeyValuePair<long, long>> GetProcessingTimeVsNRequests()
        {
            return null;
        }

        public List<KeyValuePair<long, long>> GetTravelTimeSavedVsPWTRS()
        {

            return null;
        }

        public List<KeyValuePair<long, long>> GetTravelTimeSavedVsPoolSize()
        {

            return null;
        }

        public List<KeyValuePair<long, long>> GetTravelTimeSavedVsNRequests()
        {
            return null;

        }

        public List<KeyValuePair<long, long>> GetTripsSavedVsPoolSize()
        {

            return null;
        }
    }
}
