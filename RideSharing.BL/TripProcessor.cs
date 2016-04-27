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
using System.Diagnostics;

namespace RideSharing.BL
{
    public class TripProcessor : ITripProcessor
    {
        #region Private Variables

        private List<RideDetails> rideDetails;
        private string APIUrl;
        private TripDetailsRepository tripDetailsRepo;
        private IRideProcessor rideProcessor;
        private List<long> ridesProcessed;
        private SortedDictionary<int, double> sourceMatrix;
        private List<TripDetails> tripDetails;
        private static int MAX_WALKING_RADIUS = 3;
        private static int MAX_PASSENGER_COUNT = 4;
        private static double AVG_SPEED_VEHICLE = 23;
        private static double AVG_SPEED_VEHICLE_PER_MINUTE = (AVG_SPEED_VEHICLE / 60);
        private static string SOURCE_POSITION = "-73.8056301,40.6599388;";
        private static int MIN_PERCENTAGE = 80;
        private static int MAX_PERCENTAGE = 60;
        private static double METRES_TO_MILES = 0.000621371;
        private static double MAX_WALK_PER_MINUTE = 0.05;
        private static double TRAFFIC_CORRECTION_PERCENTAGE = 10;
        private int loneRequests = 0;

        #endregion Private Variables

        #region Interface Implementation

        public TripProcessor(TripDetailsRepository TripDetailsRepo, IRideProcessor RideProcessor, string APIURL)
        {
            tripDetailsRepo = TripDetailsRepo;
            rideProcessor = RideProcessor;
            APIUrl = APIURL;
            InitializeApp();
        }

        private void InitializeApp()
        {
            rideDetails = new List<RideDetails>();
            ridesProcessed = new List<long>();
            sourceMatrix = new SortedDictionary<int, double>();
            tripDetails = new List<TripDetails>();
            loneRequests = 0;
        }

        public List<long> GetSimulationIds()
        {
            return tripDetailsRepo.GetSimulationIds();
        }

        public List<TripDetails> GetTrips(long SimulationId)
        {
            return tripDetailsRepo.GetSimulations(SimulationId);
        }

        public bool ProcessTrips(long SimulationId, string StartDateTime, string EndDateTime)
        {
            rideDetails = rideProcessor.GetRides(StartDateTime, EndDateTime);
            var rideSharingCoordinates = rideDetails.Select(r => r.Destination).ToList();
            AVG_SPEED_VEHICLE = (rideDetails.Sum(r => r.PreviousDistanceTravelled) / rideDetails.Sum(r => r.PreviousDurationTravelled / 3600)) / rideDetails.Count;
            AVG_SPEED_VEHICLE_PER_MINUTE = AVG_SPEED_VEHICLE / 60;

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
                        ride.CurrentDistanceTravelled = sourceMatrix[index] * AVG_SPEED_VEHICLE_PER_MINUTE;
                        SaveTrip(ride, cabId, SimulationId, StartDateTime, dropoffTime, 1);
                        int passengerCount = CheckForWalkingCondition(ride, dropoffTime, cabId, SimulationId, StartDateTime, 1);
                        CheckForCarSharingCondition(ride, dropoffTime, cabId, SimulationId, StartDateTime, 1, passengerCount);
                        cabId++;
                    }
                }

                Trace.WriteLine("Total Rides: " + ridesProcessed.Count);
                Trace.WriteLine("Total Cabs: " + tripDetails.GroupBy(g => g.CabId).ToList().Count);
                Trace.WriteLine("Lone Requests: " + loneRequests);

                if (WriteDataToDatabase() > 0 && ridesProcessed.Count == rideDetails.Count)
                    return true;
            }

            return false;
        }

        public bool RunSimulations(string StartDateTime, string EndDateTime, int NumSimulations, int PoolSize)
        {
            DateTime startDateTime = DateTime.Parse(StartDateTime);
            DateTime endDateTime = DateTime.Parse(EndDateTime);
            Random random = new Random();
            int startMonth = startDateTime.Month;
            int endMonth = endDateTime.Month;
            int numOfMonths = endMonth - startMonth + 1;
            int finalNumOfSimulations = NumSimulations / numOfMonths;

            int curr_month = startMonth;
            bool status = true;
            for (curr_month = startMonth; curr_month <= endMonth; ++curr_month)
            {
                for (int i = 0; i < finalNumOfSimulations && status; i++)
                {
                    InitializeApp();

                    DateTime start_date = new DateTime(startDateTime.Year, curr_month, 1);
                    DateTime curr_date = start_date.AddDays(random.Next(25));                   
                    TimeSpan startTime = TimeSpan.FromHours(6);
                    TimeSpan endTime = TimeSpan.FromHours(20);
                    int maxMinutes = (int)((endTime - startTime).TotalMinutes);
                    int minutes = random.Next(0,maxMinutes);
                    TimeSpan curr_startTime = startTime.Add(TimeSpan.FromMinutes(minutes));
                    TimeSpan curr_endTime = curr_startTime.Add(TimeSpan.FromMinutes(PoolSize));
                    var startDate = curr_date.ToString("yyyy/MM/dd ") + curr_startTime.Hours.ToString("00") + ":" + curr_startTime.Minutes.ToString("00") + ":" + curr_startTime.Seconds.ToString("00");
                    var endDate = curr_date.ToString("yyyy/MM/dd ") + curr_endTime.Hours.ToString("00") + ":" + curr_endTime.Minutes.ToString("00") + ":" + curr_endTime.Seconds.ToString("00");

                    status = ProcessTrips(Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmss")), startDate, endDate);
                }
            }
            return status;
        }

        #endregion Interface Implementation

        #region Private Methods

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
                        var timeTaken = (dmat / 60);
                        timeTaken += (TRAFFIC_CORRECTION_PERCENTAGE * timeTaken) / 100;
                        elem.TimeMatrix.Add(tempIndex++, timeTaken);
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

        private T GetDataFromServer<T>(string urlString, string method, bool steps = false)
        {
            WebClient wc = new WebClient();
            string data = "";
            if (method == "table")
            {
                data = wc.DownloadString(APIUrl + method + "/v1/driving/" + SOURCE_POSITION + urlString);
            }
            else if (method == "route" && !steps)
            {
                data = wc.DownloadString(APIUrl + method + "/v1/driving/" + urlString + "?overview=false&steps=false");
            }
            else if (method == "route")
            {
                data = wc.DownloadString(APIUrl + method + "/v1/driving/" + urlString + "?steps=true");
            }
            else if (method == "trip")
            {
                data = wc.DownloadString(APIUrl + method + "/v1/driving/" + urlString + "?steps=true");
            }

            return JsonConvert.DeserializeObject<T>(data);
        }

        private bool CheckCompatibility(RideDetails ride, string pickupDateTime, double duration, int passengerCount, bool ovveride = false)
        {
            if ((DateTime.Parse(pickupDateTime).AddMinutes(duration) <= ride.DropoffTime || ovveride) && (passengerCount + ride.PassengerCount) <= MAX_PASSENGER_COUNT)
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
            trip.PickupDateTime = DateTime.Parse(pickupDateTime);
            trip.DropoffDateTime = DateTime.Parse(dropoffDateTime);
            trip.DistanceTravelled = ride.CurrentDistanceTravelled;
            trip.SequenceNum = sequenceNum;
            tripDetails.Add(trip);

            ridesProcessed.Add(ride.RideDetailsId);
        }

        private string GetDropoffTime(string startTime, double duration)
        {
            var date = DateTime.Parse(startTime).AddMinutes(duration);
            return date.ToString();
        }

        private int CheckForWalkingCondition(RideDetails ride, string dropoffTime, int cabId, long simulationId, string startDateTime, int sequenceNum)
        {
            var compatibleWalkingRides = CheckIfAnyRidesAreWithinWalkingDistance(ride, dropoffTime);
            int passengerCount = ride.PassengerCount;
            for (int i = 0; i < compatibleWalkingRides.Count && passengerCount <= MAX_PASSENGER_COUNT; i++)
            {
                passengerCount += compatibleWalkingRides[i].PassengerCount;
                if (passengerCount <= MAX_PASSENGER_COUNT)
                {
                    compatibleWalkingRides[i].CurrentDistanceTravelled = ride.CurrentDistanceTravelled;
                    SaveTrip(compatibleWalkingRides[i], cabId, simulationId, startDateTime, dropoffTime, ++sequenceNum);
                }
                else
                {
                    passengerCount -= compatibleWalkingRides[i].PassengerCount;
                }
            }
            return passengerCount;
        }

        private double GetDistance(List<RideSharingPosition> positions)
        {
            var query = GetCoordinatesAsString(positions);
            OSRMRoute route = GetDataFromServer<OSRMRoute>(query, "route");
            return route.routes[0].distance * METRES_TO_MILES;
        }

        private void CheckForCarSharingCondition(RideDetails ride, string dropoffTime, int cabId, long simulationId, string startDateTime, int sequenceNUm, int passengerCount)
        {
            List<RideSharingPosition> positions = new List<RideSharingPosition>();
            positions.Add(new RideSharingPosition() { Latitude = 40.6599388, Longitude = -73.8056301 });
            positions.Add(ride.Destination);

            var distance = GetDistance(positions);
            ride.CurrentDistanceTravelled = distance;

            var compatibleRides = CheckIfAnyRidesAreWithinRadius(ride, dropoffTime, passengerCount);
            if (compatibleRides.Count > 0)
            {
                compatibleRides = new List<RideDetails>(MergeCommonTrips(ride, compatibleRides, dropoffTime, passengerCount));
                for (int i = 0; i < compatibleRides.Count && i < 2; i++)
                {
                    var tempDropoffDateTime = GetDropoffTime(dropoffTime, compatibleRides[i].CurrentDurationTravelled);
                    SaveTrip(compatibleRides[i], cabId, simulationId, startDateTime, tempDropoffDateTime, ++sequenceNUm);
                }
            }
        }

        private long WriteDataToDatabase()
        {
            return tripDetailsRepo.StoreTrips(tripDetails);
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

        private List<RideDetails> CheckIfAnyRidesAreWithinRadius(RideDetails ride, string pickupDateTime, int passengerCount)
        {
            SortedList<DateTime, RideDetails> tempRides = new SortedList<DateTime, RideDetails>(new DuplicateKeyComparer<DateTime>());
            double radiusToSearch = ((((MAX_PERCENTAGE * ride.CurrentDistanceTravelled) / 100) * 3) - ride.CurrentDistanceTravelled);

            foreach (var rideDict in ride.TimeMatrix.Where(t => t.Value < (radiusToSearch / AVG_SPEED_VEHICLE_PER_MINUTE) / 2))
            {
                if (rideDict.Key != 0)
                {
                    var tempRide = rideDetails[rideDict.Key - 1];
                    if (!ridesProcessed.Exists(r => r == tempRide.RideDetailsId)
                        && CheckCompatibility(tempRide, pickupDateTime, rideDict.Value, passengerCount))
                    {
                        tempRides.Add(tempRide.DropoffTime, tempRide);
                    }
                }
            }
            return tempRides.Values.ToList();
        }

        private List<RideDetails> MergeCommonTrips(RideDetails sourceRide, List<RideDetails> rides, string pickupDateTime, int passengerCount)
        {
            List<RideDetails> selectedRides = new List<RideDetails>();
            int requestsChecked = 0;
            int maxlimit = rides.Count;

            if (rides.Count > 40)
            {
                maxlimit = (rides.Count * 20) / 100;
            }

            if (rides.Count > 1)
            {
                for (int i = 1; i < rides.Count; i++)
                {
                    requestsChecked = 0;
                    for (int j = i; j < rides.Count && requestsChecked++ < maxlimit; j++)
                    {
                        if (rides[i] != rides[j] && (passengerCount + rides[i].PassengerCount + rides[j].PassengerCount <= MAX_PASSENGER_COUNT))
                        {
                            var tempSelectedRides = CheckForDirectToDestinationRequest(sourceRide, rides[i], rides[j]);
                            selectedRides = CheckForRideShareCompatibility(tempSelectedRides, sourceRide, pickupDateTime);

                            if (selectedRides.Count > 0)
                            {
                                return selectedRides;
                            }

                            RideSharingPosition centroid = new RideSharingPosition();
                            centroid.Latitude = (sourceRide.Destination.Latitude + rides[i].Destination.Latitude + rides[j].Destination.Latitude) / 3;
                            centroid.Longitude = (sourceRide.Destination.Longitude + rides[i].Destination.Longitude + rides[j].Destination.Longitude) / 3;
                            tempSelectedRides = CheckForTriangleProperty(sourceRide, rides[i], rides[j], centroid);

                            selectedRides = CheckForRideShareCompatibility(tempSelectedRides, sourceRide, pickupDateTime);

                            if (selectedRides.Count > 0)
                            {
                                return selectedRides;
                            }
                        }
                    }
                }
            }
            else if (rides.Count == 1)
            {
                loneRequests++;
            }

            return new List<RideDetails>();
        }

        private List<RideDetails> CheckForDirectToDestinationRequest(RideDetails sourceRide, RideDetails point1, RideDetails point2)
        {
            double remainingDistance = (((MAX_PERCENTAGE * sourceRide.CurrentDistanceTravelled) / 100) * 3) - sourceRide.CurrentDistanceTravelled;
            List<RideDetails> rideDetails = new List<RideDetails>();

            double source2Point1 = sourceRide.TimeMatrix[rideDetails.FindIndex(r => r.RideDetailsId == point1.RideDetailsId) + 1] * AVG_SPEED_VEHICLE_PER_MINUTE;
            double source2Point2 = sourceRide.TimeMatrix[rideDetails.FindIndex(r => r.RideDetailsId == point2.RideDetailsId) + 1] * AVG_SPEED_VEHICLE_PER_MINUTE;
            double point12Point2 = point1.TimeMatrix[rideDetails.FindIndex(r => r.RideDetailsId == point2.RideDetailsId) + 1] * AVG_SPEED_VEHICLE_PER_MINUTE;
            double point22Point1 = point2.TimeMatrix[rideDetails.FindIndex(r => r.RideDetailsId == point1.RideDetailsId) + 1] * AVG_SPEED_VEHICLE_PER_MINUTE;

            if (source2Point1 + point12Point2 <= remainingDistance)
            {
                point1.CurrentDistanceTravelled = source2Point1;
                point2.CurrentDistanceTravelled = source2Point1 + point12Point2;

                point1.CurrentDurationTravelled = point1.CurrentDistanceTravelled / AVG_SPEED_VEHICLE_PER_MINUTE;
                point2.CurrentDurationTravelled = point2.CurrentDistanceTravelled / AVG_SPEED_VEHICLE_PER_MINUTE;

                rideDetails.Add(point1);
                rideDetails.Add(point2);
            }

            if (source2Point2 + point22Point1 <= remainingDistance)
            {
                point1.CurrentDistanceTravelled = source2Point2 + point22Point1;
                point2.CurrentDistanceTravelled = source2Point2;

                point1.CurrentDurationTravelled = point1.CurrentDistanceTravelled / AVG_SPEED_VEHICLE_PER_MINUTE;
                point2.CurrentDurationTravelled = point2.CurrentDistanceTravelled / AVG_SPEED_VEHICLE_PER_MINUTE;

                rideDetails.Add(point2);
                rideDetails.Add(point1);
            }
            return rideDetails;
        }

        private List<RideDetails> CheckForRideShareCompatibility(List<RideDetails> rides, RideDetails sourceRide, string pickupDateTime)
        {
            List<RideDetails> returnData = new List<RideDetails>();
            bool reverse = false;
            for (int k = 0; k < rides.Count; k += 2)
            {
                if (CheckCompatibility(rides[k], pickupDateTime, rides[k].CurrentDurationTravelled, sourceRide.PassengerCount)
                        && CheckCompatibility(rides[k + 1], pickupDateTime, rides[k + 1].CurrentDurationTravelled, sourceRide.PassengerCount + rides[k].PassengerCount))
                {
                    if (!reverse)
                    {
                        returnData.Add(rides[k]);
                        returnData.Add(rides[k + 1]);
                    }
                    else
                    {
                        returnData.Add(rides[k + 1]);
                        returnData.Add(rides[k]);
                    }
                }
                reverse = true;
            }

            return returnData;
        }

        private List<RideDetails> CheckForTriangleProperty(RideDetails sourceRide, RideDetails point1, RideDetails point2, RideSharingPosition centroid)
        {
            double remainingDistance = (((MAX_PERCENTAGE * sourceRide.CurrentDistanceTravelled) / 100) * 3) - sourceRide.CurrentDistanceTravelled;
            List<RideDetails> rideDetails = new List<RideDetails>();

            List<RideSharingPosition> positions = new List<RideSharingPosition>();
            positions.Add(sourceRide.Destination);
            positions.Add(centroid);
            var query = GetCoordinatesAsString(positions);
            OSRMRoute route = GetDataFromServer<OSRMRoute>(query, "route");
            double sourceCentroidDist = route.routes[0].distance * METRES_TO_MILES;
            double sourceCentroidTime = route.routes[0].duration / 60;
            sourceCentroidTime += (TRAFFIC_CORRECTION_PERCENTAGE * sourceCentroidTime) / 100;

            positions = new List<RideSharingPosition>();
            positions.Add(centroid);
            positions.Add(point1.Destination);
            query = GetCoordinatesAsString(positions);
            route = GetDataFromServer<OSRMRoute>(query, "route", true);
            TriangleProperty point1Triangle = GetTrianglePropertyDetails(centroid, point1, route);
            point1.Destination = point1Triangle.Position;

            positions = new List<RideSharingPosition>();
            positions.Add(centroid);
            positions.Add(point2.Destination);
            query = GetCoordinatesAsString(positions);
            route = GetDataFromServer<OSRMRoute>(query, "route", true);
            TriangleProperty point2Triangle = GetTrianglePropertyDetails(centroid, point2, route);
            point2.Destination = point2Triangle.Position;

            if ((sourceCentroidDist + point1Triangle.DriveDistance + point2Triangle.DriveDistance) <= remainingDistance)
            {
                if (point1Triangle.DriveDistance > 0 && point2Triangle.DriveDistance > 0)
                {
                    positions = new List<RideSharingPosition>();
                    positions.Add(point1Triangle.Position);
                    positions.Add(point2Triangle.Position);
                    query = GetCoordinatesAsString(positions);
                    OSRMTrip trip = GetDataFromServer<OSRMTrip>(query, "trip");

                    double p12p2Distance = trip.trips[0].legs[0].distance * METRES_TO_MILES;
                    double p12p2Time = trip.trips[0].legs[0].duration / 60;
                    p12p2Time += (TRAFFIC_CORRECTION_PERCENTAGE * p12p2Time) / 100;

                    double p22p1Distance = trip.trips[0].legs[1].distance * METRES_TO_MILES;
                    double p22p1Time = trip.trips[0].legs[1].duration / 60;
                    p22p1Time += (TRAFFIC_CORRECTION_PERCENTAGE * p22p1Time) / 100;

                    var newPoint1 = (RideDetails)point1.Clone();
                    point1.CurrentDistanceTravelled = sourceCentroidDist + point1Triangle.DriveDistance;
                    point1.CurrentDurationTravelled = sourceCentroidTime + point1Triangle.DriveTime;

                    newPoint1.CurrentDistanceTravelled = sourceRide.CurrentDistanceTravelled + sourceCentroidDist + point2Triangle.DriveDistance + p22p1Distance;
                    newPoint1.CurrentDurationTravelled = sourceCentroidTime + point2Triangle.DriveTime + p22p1Time;


                    var newPoint2 = (RideDetails)point2.Clone();
                    point2.CurrentDistanceTravelled = sourceCentroidDist + point2Triangle.DriveDistance;
                    point2.CurrentDurationTravelled = sourceCentroidTime + point2Triangle.DriveTime;

                    newPoint2.CurrentDistanceTravelled = sourceCentroidDist + point1Triangle.DriveDistance + p12p2Distance;
                    newPoint2.CurrentDurationTravelled = sourceCentroidTime + point1Triangle.DriveTime + p12p2Time;

                    if (sourceCentroidDist + newPoint2.CurrentDistanceTravelled <= remainingDistance)
                    {
                        rideDetails.Add(point1);
                        rideDetails.Add(newPoint2);
                    }

                    if (sourceCentroidDist + point2.CurrentDistanceTravelled <= remainingDistance)
                    {
                        rideDetails.Add(newPoint1);
                        rideDetails.Add(point2);
                    }
                }
                else
                {
                    point1.CurrentDistanceTravelled = sourceCentroidDist + point1Triangle.DriveDistance;
                    point1.CurrentDurationTravelled = sourceCentroidTime + point1Triangle.DriveTime;
                    rideDetails.Add(point1);

                    point2.CurrentDistanceTravelled = sourceCentroidDist + point2Triangle.DriveDistance;
                    point2.CurrentDurationTravelled = sourceCentroidTime + point2Triangle.DriveTime;
                    rideDetails.Add(point2);
                }
            }

            return rideDetails;
        }

        private TriangleProperty GetTrianglePropertyDetails(RideSharingPosition centroid, RideDetails ride, OSRMRoute route)
        {
            TriangleProperty triangle = new TriangleProperty();

            route.routes[0].legs[0].steps.Reverse();
            triangle.DriveDistance = route.routes[0].distance * METRES_TO_MILES;
            triangle.DriveTime = route.routes[0].duration / 60;
            triangle.DriveTime += (TRAFFIC_CORRECTION_PERCENTAGE * triangle.DriveTime) / 100;

            foreach (var r in route.routes[0].legs[0].steps)
            {
                if (triangle.WalkDistance + (r.distance * METRES_TO_MILES) <= (ride.WalkTime * MAX_WALK_PER_MINUTE))
                {
                    triangle.WalkDistance += (r.distance * METRES_TO_MILES);
                    triangle.DriveTime -= (r.duration / 60);
                    triangle.DriveTime += (TRAFFIC_CORRECTION_PERCENTAGE * triangle.DriveTime) / 100;
                    triangle.Position.Latitude = r.maneuver.location[1];
                    triangle.Position.Longitude = r.maneuver.location[0];
                }
                else
                {
                    break;
                }
            }

            triangle.DriveDistance -= triangle.WalkDistance;
            if (triangle.DriveDistance <= 0)
            {
                triangle.DriveDistance = 0;
                triangle.DriveTime = 0;
                triangle.Position = centroid;
            }

            return triangle;
        }

        #endregion Private Methods
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
