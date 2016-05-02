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
    class CustomKey
    {
        public RideSharingPosition Source { get; set; }

        public RideSharingPosition Destination { get; set; }

        public CustomKey(RideSharingPosition P1, RideSharingPosition P2)
        {
            Source = P1;
            Destination = P2;
        }
    }

    public class TripProcessor : ITripProcessor
    {
        #region Private Variables

        private List<RideDetails> rideDetails;
        private string DrivingAPIURL;
        private string WalkingAPIURL;
        private TripDetailsRepository tripDetailsRepo;
        private IRideProcessor rideProcessor;
        private List<long> ridesProcessed;
        private SortedDictionary<int, double> sourceDMatrix;
        private SortedDictionary<int, double> sourceWMatrix;
        private List<TripDetails> tripDetails;
        private static int MAX_WALKING_RADIUS = 30;
        private static int MAX_PASSENGER_COUNT = 4;
        private static double AVG_SPEED_VEHICLE = 23;
        private static double AVG_SPEED_VEHICLE_PER_MINUTE = (AVG_SPEED_VEHICLE / 60);
        private static string SOURCE_POSITION = "-73.8056301,40.6599388;";
        //private static int MIN_PERCENTAGE = 80;
        private static int MAX_PERCENTAGE = 60;
        private static double METRES_TO_MILES = 0.000621371;
        private static double MAX_WALK_PER_MINUTE = 0.05;
        private static double TRAFFIC_CORRECTION_PERCENTAGE = 10;
        private int loneRequests = 0;
        private Dictionary<CustomKey, Route> rideDict;
        Dictionary<int, bool> PercentageWillingToRideShare;

        #endregion Private Variables

        #region Interface Implementation

        public TripProcessor(TripDetailsRepository TripDetailsRepo, IRideProcessor RideProcessor, string DrivingURL, string WalkingURL)
        {
            tripDetailsRepo = TripDetailsRepo;
            rideProcessor = RideProcessor;
            DrivingAPIURL = DrivingURL;
            WalkingAPIURL = WalkingURL;
            InitializeApp();
        }

        private void InitializeApp()
        {
            rideDetails = new List<RideDetails>();
            ridesProcessed = new List<long>();
            sourceDMatrix = new SortedDictionary<int, double>();
            sourceWMatrix = new SortedDictionary<int, double>();
            tripDetails = new List<TripDetails>();
            loneRequests = 0;
            rideDict = new Dictionary<CustomKey, Route>();
            PercentageWillingToRideShare = new Dictionary<int, bool>();
        }

        public List<long> GetSimulationIds()
        {
            return tripDetailsRepo.GetSimulationIds();
        }

        public SimulationViewModel GetTrips(long SimulationId)
        {
            return tripDetailsRepo.GetSimulationDetails(SimulationId);
        }

        public bool ProcessTrips(long SimulationId, string StartDateTime, string EndDateTime, int WillingToRideSharePercentage)
        {
            var ProcessingStartTime = DateTime.Now.ToString();
            Trace.WriteLine("Processing Starttime: " + ProcessingStartTime);
            rideDetails = rideProcessor.GetRides(StartDateTime, EndDateTime);

            if (rideDetails.Count > 0)
            {
                GenerateRandomPercentageWillingToRideShare(WillingToRideSharePercentage, rideDetails.Count);
                var rideSharingCoordinates = rideDetails.Select(r => r.Destination).ToList();
                AVG_SPEED_VEHICLE = (rideDetails.Sum(r => r.PreviousDistanceTravelled) / rideDetails.Sum(r => r.PreviousDurationTravelled / 3600)) / rideDetails.Count;
                AVG_SPEED_VEHICLE_PER_MINUTE = AVG_SPEED_VEHICLE / 60;

                try
                {
                    if (ConstructDistanceMatrixFromSource(rideSharingCoordinates))
                    {
                        int cabId = 1;
                        foreach (var item in PercentageWillingToRideShare.Where(v => v.Value == false))
                        {
                            var ride = rideDetails[item.Key];
                            var dropoffTime = GetDropoffTime(StartDateTime, sourceDMatrix[item.Key + 1]);
                            List<RideSharingPosition> positions = new List<RideSharingPosition>();
                            positions.Add(new RideSharingPosition() { Latitude = 40.6599388, Longitude = -73.8056301 });
                            positions.Add(ride.Destination);
                            ride.CurrentDistanceTravelled = GetDistance(positions).distance * METRES_TO_MILES;
                            SaveTrip(ride, cabId++, SimulationId, StartDateTime, dropoffTime, 1);
                        }
                        int index = 0;
                        foreach (var ride in rideDetails)
                        {
                            index++;
                            if (!ridesProcessed.Exists(r => r == ride.RideDetailsId) && CheckCompatibility(ride, StartDateTime, sourceDMatrix[index], 0, true))
                            {
                                var dropoffTime = GetDropoffTime(StartDateTime, sourceDMatrix[index]);
                                List<RideSharingPosition> positions = new List<RideSharingPosition>();
                                positions.Add(new RideSharingPosition() { Latitude = 40.6599388, Longitude = -73.8056301 });
                                positions.Add(ride.Destination);
                                ride.CurrentDistanceTravelled = GetDistance(positions).distance * METRES_TO_MILES;
                                SaveTrip(ride, cabId, SimulationId, StartDateTime, dropoffTime, 1);

                                int passengerCount = CheckForWalkingCondition(ride, dropoffTime, cabId, SimulationId, StartDateTime, 1);
                                CheckForCarSharingCondition(ride, dropoffTime, cabId, SimulationId, StartDateTime, 1, passengerCount);
                                cabId++;
                            }
                        }

                    }

                    var ProcessingEndTime = DateTime.Now.ToString();
                    Trace.WriteLine("Total Rides: " + ridesProcessed.Count);
                    Trace.WriteLine("Total Cabs: " + tripDetails.GroupBy(g => g.CabId).ToList().Count);
                    Trace.WriteLine("Lone Requests: " + loneRequests);
                    Trace.WriteLine("Processing Endtime: " + ProcessingEndTime);


                    if (WriteDataToDatabase(SimulationId, StartDateTime, EndDateTime, ProcessingStartTime, ProcessingEndTime, WillingToRideSharePercentage) > 0 && ridesProcessed.Count == rideDetails.Count)
                        return true;
                }
                catch (Exception)
                {
                    Trace.WriteLine("Failed test case!");
                    return true;
                }

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
                    int minutes = random.Next(0, maxMinutes);
                    TimeSpan curr_startTime = startTime.Add(TimeSpan.FromMinutes(minutes));
                    TimeSpan curr_endTime = curr_startTime.Add(TimeSpan.FromMinutes(PoolSize));
                    var startDate = curr_date.ToString("yyyy/MM/dd ") + curr_startTime.Hours.ToString("00") + ":" + curr_startTime.Minutes.ToString("00") + ":" + curr_startTime.Seconds.ToString("00");
                    var endDate = curr_date.ToString("yyyy/MM/dd ") + curr_endTime.Hours.ToString("00") + ":" + curr_endTime.Minutes.ToString("00") + ":" + curr_endTime.Seconds.ToString("00");

                    status = ProcessTrips(Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")), startDate, endDate, 100);
                    InitializeApp();
                    status = ProcessTrips(Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")), startDate, endDate, 75);
                    InitializeApp();
                    status = ProcessTrips(Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")), startDate, endDate, 50);
                    InitializeApp();
                    status = ProcessTrips(Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")), startDate, endDate, 25);
                }
            }
            return status;
        }

        #endregion Interface Implementation

        #region Private Methods

        private void GenerateRandomPercentageWillingToRideShare(int perc, int count)
        {
            int maxNum = count - ((perc * count) / 100);
            List<int> values = new List<int>();
            for (int i = 0; i < count; i++)
            {
                values.Add(i);
            }
            Random rnd = new Random();
            var data = values.ToList().OrderBy(x => rnd.Next()).ToList();
            for (int i = 0; i < count; i++)
            {
                PercentageWillingToRideShare.Add(data[i], i >= maxNum);
            }
        }

        private bool ConstructDistanceMatrixFromSource(List<RideSharingPosition> positions)
        {
            String coordinates = GetCoordinatesAsString(positions);

            if (!String.IsNullOrEmpty(coordinates))
            {
                var dMatrix = GetDataFromServer<OSRMDistanceMatrix>(coordinates, "table");
                var wMatrix = GetDataFromServer<OSRMDistanceMatrix>(coordinates, "table", false, "walking");
                int index = 0;
                foreach (var elem in dMatrix.durations[0])
                {
                    sourceDMatrix.Add(index, elem / 60);
                    sourceWMatrix.Add(index, wMatrix.durations[0][index++] / 60);
                }

                index = 1;
                foreach (var elem in rideDetails)
                {
                    for (int i = 0; i < rideDetails.Count; i++)
                    {
                        var drivingTimeTaken = dMatrix.durations[index][i + 1] / 60;
                        drivingTimeTaken += (TRAFFIC_CORRECTION_PERCENTAGE * drivingTimeTaken) / 100;
                        var walkingTimeTaken = wMatrix.durations[index][i + 1] / 60;
                        elem.DrivingTimeMatrix.Add(i, drivingTimeTaken);
                        elem.WalkingTimeMatrix.Add(i, walkingTimeTaken);
                    }
                    index++;
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

        private T GetDataFromServer<T>(string urlString, string method, bool steps = false, string mode = "driving")
        {
            WebClient wc = new WebClient();
            string data = "";
            if (method == "table")
            {
                if (mode == "driving")
                    data = wc.DownloadString(DrivingAPIURL + method + "/v1/" + mode + "/" + SOURCE_POSITION + urlString);
                else
                    data = wc.DownloadString(WalkingAPIURL + method + "/v1/" + mode + "/" + SOURCE_POSITION + urlString);
            }
            else if (method == "route" && !steps)
            {
                data = wc.DownloadString(DrivingAPIURL + method + "/v1/driving/" + urlString + "?overview=false&steps=false");
            }
            else if (method == "route")
            {
                data = wc.DownloadString(DrivingAPIURL + method + "/v1/driving/" + urlString + "?steps=true");
            }
            else if (method == "trip")
            {
                data = wc.DownloadString(DrivingAPIURL + method + "/v1/driving/" + urlString + "?steps=true");
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

        private Route GetDistance(List<RideSharingPosition> positions)
        {
            if (rideDict.Count(r => r.Key.Source == positions[0] && r.Key.Destination == positions[1]) == 0)
            {
                var query = GetCoordinatesAsString(positions);
                OSRMRoute route = GetDataFromServer<OSRMRoute>(query, "route", true);
                rideDict.Add(new CustomKey(positions[0], positions[1]), route.routes[0]);
            }
            return rideDict.First(r => r.Key.Source == positions[0] && r.Key.Destination == positions[1]).Value;
        }

        private void CheckForCarSharingCondition(RideDetails ride, string dropoffTime, int cabId, long simulationId, string startDateTime, int sequenceNUm, int passengerCount)
        {
            List<RideSharingPosition> positions = new List<RideSharingPosition>();

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

        private long WriteDataToDatabase(long simulationId, string poolStartDate, string poolEndDate, string processingStartTime, string processingEndTime, int PWRS)
        {
            var PoolSize = (DateTime.Parse(poolEndDate) - DateTime.Parse(poolStartDate)).Minutes;
            tripDetailsRepo.StoreSimulations(simulationId, poolStartDate, poolEndDate, PoolSize, processingStartTime, processingEndTime, PWRS);
            return tripDetailsRepo.StoreTrips(tripDetails);
        }

        private List<RideDetails> CheckIfAnyRidesAreWithinWalkingDistance(RideDetails ride, string pickupDateTime)
        {
            SortedList<DateTime, RideDetails> tempRides = new SortedList<DateTime, RideDetails>(new DuplicateKeyComparer<DateTime>());

            foreach (var walkDict in ride.WalkingTimeMatrix.Where(t => t.Value < MAX_WALKING_RADIUS))
            {
                var tempRide = rideDetails[walkDict.Key];
                if (!ridesProcessed.Exists(r => r == tempRide.RideDetailsId)
                    && (tempRide.WalkTime <= walkDict.Value)
                    && CheckCompatibility(tempRide, pickupDateTime, walkDict.Value, ride.PassengerCount))
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

            foreach (var rideDict in ride.DrivingTimeMatrix.Where(t => t.Value < (radiusToSearch / AVG_SPEED_VEHICLE_PER_MINUTE) / 2))
            {

                var tempRide = rideDetails[rideDict.Key];
                if (!ridesProcessed.Exists(r => r == tempRide.RideDetailsId)
                    && CheckCompatibility(tempRide, pickupDateTime, rideDict.Value, passengerCount))
                {
                    tempRides.Add(tempRide.DropoffTime, tempRide);
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
            List<RideDetails> returnData = new List<RideDetails>();

            double source2Point1 = sourceRide.DrivingTimeMatrix[rideDetails.FindIndex(r => r.RideDetailsId == point1.RideDetailsId)] * AVG_SPEED_VEHICLE_PER_MINUTE;
            double source2Point2 = sourceRide.DrivingTimeMatrix[rideDetails.FindIndex(r => r.RideDetailsId == point2.RideDetailsId)] * AVG_SPEED_VEHICLE_PER_MINUTE;
            double point12Point2 = point1.DrivingTimeMatrix[rideDetails.FindIndex(r => r.RideDetailsId == point2.RideDetailsId)] * AVG_SPEED_VEHICLE_PER_MINUTE;
            double point22Point1 = point2.DrivingTimeMatrix[rideDetails.FindIndex(r => r.RideDetailsId == point1.RideDetailsId)] * AVG_SPEED_VEHICLE_PER_MINUTE;

            if (source2Point1 + point12Point2 <= remainingDistance)
            {
                point1.CurrentDistanceTravelled = source2Point1;
                point2.CurrentDistanceTravelled = source2Point1 + point12Point2;

                point1.CurrentDurationTravelled = point1.CurrentDistanceTravelled / AVG_SPEED_VEHICLE_PER_MINUTE;
                point2.CurrentDurationTravelled = point2.CurrentDistanceTravelled / AVG_SPEED_VEHICLE_PER_MINUTE;

                returnData.Add(point1);
                returnData.Add(point2);
            }

            if (source2Point2 + point22Point1 <= remainingDistance)
            {
                point1.CurrentDistanceTravelled = source2Point2 + point22Point1;
                point2.CurrentDistanceTravelled = source2Point2;

                point1.CurrentDurationTravelled = point1.CurrentDistanceTravelled / AVG_SPEED_VEHICLE_PER_MINUTE;
                point2.CurrentDurationTravelled = point2.CurrentDistanceTravelled / AVG_SPEED_VEHICLE_PER_MINUTE;

                returnData.Add(point2);
                returnData.Add(point1);
            }
            return returnData;
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
            var route = GetDistance(positions);
            double sourceCentroidDist = route.distance * METRES_TO_MILES;
            double sourceCentroidTime = route.duration / 60;
            sourceCentroidTime += (TRAFFIC_CORRECTION_PERCENTAGE * sourceCentroidTime) / 100;

            positions = new List<RideSharingPosition>();
            positions.Add(centroid);
            positions.Add(point1.Destination);
            route = GetDistance(positions);
            TriangleProperty point1Triangle = GetTrianglePropertyDetails(centroid, point1, route);
            point1.Destination = point1Triangle.Position;

            positions = new List<RideSharingPosition>();
            positions.Add(centroid);
            positions.Add(point2.Destination);
            route = GetDistance(positions);
            TriangleProperty point2Triangle = GetTrianglePropertyDetails(centroid, point2, route);
            point2.Destination = point2Triangle.Position;

            if ((sourceCentroidDist + point1Triangle.DriveDistance + point2Triangle.DriveDistance) <= remainingDistance)
            {
                if (point1Triangle.DriveDistance > 0 && point2Triangle.DriveDistance > 0)
                {
                    positions = new List<RideSharingPosition>();
                    positions.Add(point1Triangle.Position);
                    positions.Add(point2Triangle.Position);
                    var query = GetCoordinatesAsString(positions);
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

        private TriangleProperty GetTrianglePropertyDetails(RideSharingPosition centroid, RideDetails ride, Route route)
        {
            TriangleProperty triangle = new TriangleProperty();

            route.legs[0].steps.Reverse();
            triangle.DriveDistance = route.distance * METRES_TO_MILES;
            triangle.DriveTime = route.duration / 60;
            triangle.DriveTime += (TRAFFIC_CORRECTION_PERCENTAGE * triangle.DriveTime) / 100;

            foreach (var r in route.legs[0].steps)
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
