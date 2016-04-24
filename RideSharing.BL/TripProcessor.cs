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
        private static int MAX_PASSENGER_COUNT = 4;
        private static double AVG_SPEED_VEHICLE = 45;
        private static double AVG_SPEED_VEHICLE_PER_MINUTE = (AVG_SPEED_VEHICLE / 60);
        private static string SOURCE_POSITION = "-73.8056301,40.6599388;";
        private static int MIN_PERCENTAGE = 80;
        private static int MAX_PERCENTAGE = 60;

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
                        SaveTrip(ride, cabId, SimulationId, StartDateTime, dropoffTime, 1);
                        CheckForWalkingCondition(ride, dropoffTime, cabId, SimulationId, StartDateTime, 1);
                        CheckForCarSharingCondition(ride, dropoffTime, cabId, SimulationId, StartDateTime, 1);
                        cabId++;
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
                var dMatrix = GetDataFromServer<OSRMDistanceMatrix>(coordinates, "table");

                int index = 0;
                foreach (var elem in dMatrix.durations[0])
                {
                    sourceMatrix.Add(index++, elem / 60);
                }

                index = 1;
                foreach (var elem in rideDetails)
                {
                    int tempIndex = 0;
                    foreach (double dmat in dMatrix.durations[index++])
                    {
                        elem.TimeMatrix.Add(tempIndex++, dmat / 60);
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

        private T GetDataFromServer<T>(string urlString, string method)
        {
            WebClient wc = new WebClient();
            string data = "";
            if(method == "table")
            {
                data = wc.DownloadString(APIUrl + method + "/v1/driving/" + SOURCE_POSITION + urlString);
            }
            else if (method == "route")
            {
                data = wc.DownloadString(APIUrl + method + "/v1/driving/" + urlString + "?overview=false&steps=false");
            }

            return JsonConvert.DeserializeObject<T>(data);
        }

        private bool CheckCompatibility(RideDetails ride, string pickDateTime, double duration, int passengerCount, bool ovveride = false)
        {
            if ((DateTime.Parse(pickDateTime).AddMinutes(duration) <= ride.DropoffTime || ovveride) && (passengerCount + ride.PassengerCount) <= MAX_PASSENGER_COUNT)
            {
                return true;
            }
            return false;
        }

        private void SaveTrip(RideDetails ride, int cabId, long simulationId, string pickupDateTime, string dropoffDateTime, int sequenceNum)
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
            trip.SequenceNum = sequenceNum;
            tripDetails.Add(trip);

            ridesProcessed.Add(ride.RideDetailsId);
        }

        private string GetDropoffTime(string startTime, double duration)
        {
            var date = DateTime.Parse(startTime).AddMinutes(duration);
            return date.ToString();
        }

        private void CheckForWalkingCondition(RideDetails ride, string dropoffTime, int cabId, long simulationId, string startDateTime, int sequenceNum)
        {
            var compatibleWalkingRides = CheckIfAnyRidesAreWithinWalkingDistance(ride, dropoffTime);
            for (int i = 0; i < compatibleWalkingRides.Count && i < 2; i++)
            {
                SaveTrip(compatibleWalkingRides[i], cabId, simulationId, startDateTime, dropoffTime, ++sequenceNum);
            }
        }

        private double GetDistance(List<RideSharingPosition> positions)
        {
            var query = GetCoordinatesAsString(positions);
            OSRMRoute route = GetDataFromServer<OSRMRoute>(query, "route");
            return route.routes[0].distance * 0.000621371;
        }

        private void CheckForCarSharingCondition(RideDetails ride, string dropoffTime, int cabId, long simulationId, string startDateTime, int sequenceNUm)
        {
            List<RideSharingPosition> positions = new List<RideSharingPosition>();
            positions.Add(new RideSharingPosition() { Latitude = 40.6599388, Longitude = -73.8056301 });
            positions.Add(ride.Destination);
            var distance = GetDistance(positions);

            var compatibleRides = CheckIfAnyRidesAreWithinRadius(ride, dropoffTime, distance);
            for (int i = 0; i < compatibleRides.Count && i < 2; i++)
            {
                SaveTrip(compatibleRides[i], cabId, simulationId, startDateTime, dropoffTime, ++sequenceNUm);
            }
        }

        private List<RideDetails> CheckIfAnyRidesAreWithinWalkingDistance(RideDetails ride, string pickupDateTime)
        {
            SortedList<DateTime, RideDetails> tempRides = new SortedList<DateTime, RideDetails>(new DuplicateKeyComparer<DateTime>());

            foreach (var rideDict in ride.TimeMatrix.Where(t => t.Value < MAX_WALKING_RADIUS))
            {
                var tempRide = rideDetails[rideDict.Key - 1];
                if (!ridesProcessed.Exists(r => r == tempRide.RideDetailsId) 
                    && (tempRide.WalkTime <= (rideDict.Value * AVG_SPEED_VEHICLE_PER_MINUTE))
                    && CheckCompatibility(tempRide, pickupDateTime, rideDict.Value, ride.PassengerCount))
                {
                    tempRides.Add(tempRide.DropoffTime, tempRide);
                }
            }

            return tempRides.Values.ToList();
        }

        private List<RideDetails> CheckIfAnyRidesAreWithinRadius(RideDetails ride, string pickupDateTime, double distance)
        {
            SortedList<DateTime, RideDetails> tempRides = new SortedList<DateTime, RideDetails>(new DuplicateKeyComparer<DateTime>());
            double remainingDistanceMin = (((MIN_PERCENTAGE * distance) / 100) * 2) - distance;
            double remainingDistanceMax = (((MAX_PERCENTAGE * distance) / 100) * 3) - distance;

            foreach (var rideDict in ride.TimeMatrix.Where(t => t.Value < (remainingDistanceMin / AVG_SPEED_VEHICLE_PER_MINUTE)))
            {
                var tempRide = rideDetails[rideDict.Key - 1];
                if(!ridesProcessed.Exists(r => r == tempRide.RideDetailsId) && CheckCompatibility(tempRide, pickupDateTime, rideDict.Value, ride.PassengerCount))
                {
                    tempRides.Add(tempRide.DropoffTime, tempRide);
                }
            }         

            return tempRides.Values.ToList(); 
        }

        private List<RideDetails> CheckForTriangleProperty(List<RideDetails> rides, string pickupDateTime, double remainingDistance)
        {


            return new List<RideDetails>();
        }
    }


    public class DuplicateKeyComparer<TKey> :
             IComparer<TKey> where TKey : IComparable
    {
        public int Compare(TKey x, TKey y)
        {
            int result = x.CompareTo(y);

            if (result == 0)
                return 1;
            else
                return result;
        }
    }
}
