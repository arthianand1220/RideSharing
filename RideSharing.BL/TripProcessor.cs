using RideSharing.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RideSharing.Models;
using RideSharing.DAL;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using Microsoft.SqlServer.Types;

namespace RideSharing.BL
{
    public class DuplicateKeyComparer<TKey> :
             IComparer<TKey> where TKey : IComparable
    {
        #region IComparer<TKey> Members

        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1;   // Handle equality as beeing greater
            else
                return result;
        }

        #endregion
    }

    public class TripProcessor : ITripProcessor
    {
        private List<RideDetails> rideDetails;
        private string APIUrl;
        private RideDetailsRepository rideDetailsRepo;
        private IRideProcessor rideProcessor;
        private List<long> ridesProcessed;
        private SortedDictionary<int, double> sourceMatrix;
        private List<TripDetails> tripDetails;
        private static int MAX_WALKING_RADIUS = 3;
        private static int MAX_PASSENGER_COUNT = 6;
        private static double AVG_SPEED_VEHICLE_PER_MINUTE = (60 / 60);
        private static string SOURCE_POSITION = "-73.8056301,40.6599388;";

        public TripProcessor(RideDetailsRepository RideDetailsRepo, IRideProcessor RideProcessor, string APIURL)
        {
            rideDetailsRepo = RideDetailsRepo;
            rideProcessor = RideProcessor;
            APIUrl = APIURL;
            rideDetails = new List<RideDetails>();
            ridesProcessed = new List<long>();
            sourceMatrix = new SortedDictionary<int, double>();
            tripDetails = new List<TripDetails>();
        }

        public List<TripDetails> GetTrips(long SimulationId)
        {
            throw new NotImplementedException();
        }

        public bool ProcessTrips(long SimulationId, string StartDateTime, string EndDateTime)
        {
            rideDetails = rideProcessor.GetRides(StartDateTime, EndDateTime);
            var rideSharingCoordinates = rideDetails.Select(r => r.Destination).ToList();

            if (ConstructDistanceMatrixFromSource(rideSharingCoordinates))
            {
                int index = 0;
                int cabId = 1;
                foreach (var ride in rideDetails)
                {
                    index++;
                    if (!ridesProcessed.Exists(r => r == ride.RideDetailsId) && CheckCompatibility(ride, StartDateTime, sourceMatrix[index], 0, true))
                    {
                        var dropoffTime = GetDropoffTime(StartDateTime, sourceMatrix[index]);
                        SaveTrip(ride, cabId, SimulationId, StartDateTime, dropoffTime);
                        CheckForWalkingCondition(ride, dropoffTime, cabId, SimulationId, StartDateTime);
                    }
                }

                Console.WriteLine(tripDetails.GroupBy(g => g.CabId).ToList().Count);
                Console.WriteLine(tripDetails.Sum(s => s.PassengerCount));

                if (ridesProcessed.Count == rideDetails.Count)
                    return true;
            }

            return false;
        }

        public bool RunSimulations(string StartDateTime, string EndDateTime, int NumSimulations, int PoolSize)
        {
            throw new NotImplementedException();
        }

        private bool ConstructDistanceMatrixFromSource(List<RideSharingPosition> positions)
        {
            String coordinates = GetCoordinatesAsString(positions);

            if (!String.IsNullOrEmpty(coordinates))
            {
                var dMatrix = GetDataFromServer<OSRMDistanceMatrix>(coordinates);

                int index = 0;
                foreach (var elem in dMatrix.durations[0])
                {
                    sourceMatrix.Add(index++, elem);
                }

                index = 1;
                foreach (var elem in rideDetails)
                {
                    int tempIndex = 0;
                    foreach (double dmat in dMatrix.durations[index++])
                    {
                        elem.TimeMatrix.Add(tempIndex++, dmat);
                    }
                }
                return true;
            }

            return false;
        }

        private string GetCoordinatesAsString(List<RideSharingPosition> positions)
        {
            string coordinates = "";

            foreach (var position in positions)
            {
                coordinates += position.Longitude + "," + position.Latitude + ";";
            }

            return coordinates.Substring(0, coordinates.Length - 1);
        }

        private T GetDataFromServer<T>(string urlString)
        {
            WebClient wc = new WebClient();
            var data = wc.DownloadString(APIUrl + "table/v1/driving/" + SOURCE_POSITION + urlString);
            return JsonConvert.DeserializeObject<T>(data);
        }

        private bool CheckCompatibility(RideDetails ride, string pickDateTime, double duration, int passengerCount, bool ovveride = false)
        {
            if ((DateTime.Parse(pickDateTime).AddSeconds(duration) <= ride.DropoffTime || ovveride) && (passengerCount + ride.PassengerCount) <= MAX_PASSENGER_COUNT)
            {
                return true;
            }
            return false;
        }

        private void SaveTrip(RideDetails ride, int cabId, long simulationId, string pickupDateTime, string dropoffDateTime)
        {
            TripDetails trip = new TripDetails();
            trip.CabId = cabId;
            trip.SimulationId = simulationId;
            trip.RideId = ride.RideDetailsId;
            trip.Destination = SqlGeography.Point(ride.Destination.Latitude, ride.Destination.Longitude, 4326);
            trip.PickupTime = DateTime.Parse(pickupDateTime);
            trip.DropoffTime = DateTime.Parse(dropoffDateTime);
            trip.DelayTime = ride.WaitTime;
            trip.WalkTime = ride.WalkTime;
            trip.PassengerCount = ride.PassengerCount;
            tripDetails.Add(trip);

            ridesProcessed.Add(ride.RideDetailsId);
        }

        private string GetDropoffTime(string startTime, double duration)
        {
            var date = DateTime.Parse(startTime).AddSeconds(duration);
            return date.ToString();
        }

        private void CheckForWalkingCondition(RideDetails ride, string dropoffTime, int cabId, long simulationId, string startDateTime)
        {
            var compatibleWalkingRides = CheckIfAnyRidesAreWithinWalkingDistance(ride, dropoffTime);
            for (int i = 0; i < compatibleWalkingRides.Count && i < 2; i++)
            {
                SaveTrip(compatibleWalkingRides[i], cabId, simulationId, startDateTime, dropoffTime);
            }
            cabId++;
        }

        private void CheckForCarSharingCondition(RideDetails ride, string dropoffTime, int cabId, long simulationId, string startDateTime)
        {
            var compatibleWalkingRides = CheckIfAnyRidesAreWithinWalkingDistance(ride, dropoffTime);
            for (int i = 0; i < compatibleWalkingRides.Count && i < 2; i++)
            {
                SaveTrip(compatibleWalkingRides[i], cabId, simulationId, startDateTime, dropoffTime);
            }
            cabId++;
        }

        private List<RideDetails> CheckIfAnyRidesAreWithinWalkingDistance(RideDetails ride, string pickupDateTime)
        {
            SortedList<DateTime, RideDetails> tempRides = new SortedList<DateTime, RideDetails>(new DuplicateKeyComparer<DateTime>());
            int tempPassengerCount = 0;

            //1 min = 0.66 miles
            //2 min = 1.3 miles
            //3 min = 1.99 miles

            foreach (var rideDict in ride.TimeMatrix.Where(t => (t.Value / 60) < MAX_WALKING_RADIUS))
            {
                var tempRide = rideDetails[rideDict.Key - 1];
                if (!ridesProcessed.Exists(r => r == tempRide.RideDetailsId) 
                    && (tempRide.WalkTime <= ((rideDict.Value / 60) * AVG_SPEED_VEHICLE_PER_MINUTE))
                    && CheckCompatibility(tempRide, pickupDateTime, rideDict.Value, ride.PassengerCount))
                {
                    tempRides.Add(tempRide.DropoffTime, tempRide);
                    tempPassengerCount += tempRide.PassengerCount;
                }
            }

            return tempRides.Values.ToList();
        }

        private List<RideDetails> CheckIfAnyRidesAreWithinSmallRadius(RideDetails ride, string pickupDateTime)
        {
            return new List<RideDetails>();
        }
    }
}
