using RideSharing.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RideSharing.Models;
using RideSharing.DAL;
using System.Net.Http;

namespace RideSharing.BL
{
    public class TripProcessor : ITripProcessor
    {
        private List<RideDetails> rideDetails;
        private Dictionary<long, float> rideDistanceMatrix;
        private string APIUrl;
        private RideDetailsRepository rideDetailsRepo;
        private IRideProcessor rideProcessor;

        public TripProcessor(RideDetailsRepository RideDetailsRepo, IRideProcessor RideProcessor, string APIURL)
        {
            rideDetailsRepo = RideDetailsRepo;
            rideProcessor = RideProcessor;
            APIUrl = APIURL;
            rideDetails = new List<RideDetails>();
            rideDistanceMatrix = new Dictionary<long, float>();
        }

        public List<TripDetails> GetTrips(long simulationId)
        {
            throw new NotImplementedException();
        }

        public bool ProcessTrips(string startDate, string endDate)
        {
            rideDetails = rideProcessor.GetRides(startDate, endDate);

            return false;
        }

        public bool RunSimulations(string startDate, string endDate, int numSimulations, int poolSize)
        {
            throw new NotImplementedException();
        }

        private void ConstructDistanceMatrixFromSource()
        {
            HttpClient httpClient = new HttpClient();
        }
    }
}
