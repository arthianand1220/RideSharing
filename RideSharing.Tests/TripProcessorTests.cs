using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RideSharing.BL;
using RideSharing.DAL;
using RideSharing.Contract;
using System.Diagnostics;

namespace RideSharing.Tests
{
    [TestClass]
    public class TripProcessorTests
    {
        string dURL = "http://192.168.0.114:5000/";
        string wURL = "http://192.168.0.111:5000/";
        RideDetailsRepository rideDetailsRepo;
        TripDetailsRepository tripDetailsRepo;
        IRideProcessor rideProcessor;
        ITripProcessor tripProcessor;

        [TestInitialize]
        public void Initialize()
        {
            rideDetailsRepo = new RideDetailsRepository();
            tripDetailsRepo = new TripDetailsRepository();
            rideProcessor = new RideProcessor(rideDetailsRepo);
            tripProcessor = new TripProcessor(tripDetailsRepo, rideProcessor, dURL, wURL);
        }

        [TestMethod]
        public void TestProcessTripsForSmallPoolSize_Valid()
        {
            Trace.WriteLine("Small Pool Size");

            Assert.AreEqual(true, tripProcessor.ProcessTrips(Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")), "06/4/2013 16:30 PM", "06/4/2013 16:35 PM"));            
        }

        [TestMethod]
        public void TestProcessTripsForMediumPoolSize_Valid()
        {
            Trace.WriteLine("Medium Pool Size");

            Assert.AreEqual(true, tripProcessor.ProcessTrips(Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")), "06/16/2013 1:10 PM", "06/16/2013 1:25 PM"));
        }

        [TestMethod]
        public void TestProcessTripsForLargePoolSize_Valid()
        {
            Trace.WriteLine("Large Pool Size");

            Assert.AreEqual(true, tripProcessor.ProcessTrips(Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")), "06/4/2013 16:00 PM", "06/4/2013 16:20 PM"));
        }

        [TestMethod]
        public void TestProcessTripsForExtraLargePoolSize_Valid()
        {
            Trace.WriteLine("Extra Large Pool Size");

            Assert.AreEqual(true, tripProcessor.ProcessTrips(Convert.ToInt64(DateTime.Now.ToString("yyyyMMddHHmmssfff")), "06/4/2013 16:00 PM", "06/4/2013 16:30 PM"));
        }

        [TestMethod]
        public void TestRunSimulationsForSmallPoolSize_Valid()
        {
            Trace.WriteLine("Run Simulations - Small Pool Size");

            Assert.AreEqual(true, tripProcessor.RunSimulations("06/01/2013 00:00 PM", "06/27/2013 16:00 PM", 30, 5));
        }

        [TestMethod]
        public void TestRunSimulationsForMediumPoolSize_Valid()
        {
            Trace.WriteLine("Run Simulations - Medium Pool Size");

            Assert.AreEqual(true, tripProcessor.RunSimulations("06/01/2013 00:00 PM", "06/27/2013 16:00 PM", 30, 10));
        }

        [TestMethod]
        public void TestRunSimulationsForLargePoolSize_Valid()
        {
            Trace.WriteLine("Run Simulations - Large Pool Size");

            Assert.AreEqual(true, tripProcessor.RunSimulations("06/01/2013 00:00 PM", "06/27/2013 16:00 PM", 30, 20));
        }

        [TestMethod]
        public void TestRunSimulationsForExtraLargePoolSize_Valid()
        {
            Trace.WriteLine("Run Simulations - Extra Large Pool Size");

            Assert.AreEqual(true, tripProcessor.RunSimulations("06/01/2013 00:00 PM", "06/27/2013 16:00 PM", 30, 30));
        }

        [TestMethod]
        public void TestRunSimulationsForAllPoolSizes_Valid()
        {
            Trace.WriteLine("Run Simulations - All Pool Sizes");

            int NumOfTestCases = 30;

            Assert.AreEqual(true, tripProcessor.RunSimulations("06/01/2013 00:00 PM", "06/27/2013 16:00 PM", NumOfTestCases, 5));
            Assert.AreEqual(true, tripProcessor.RunSimulations("06/01/2013 00:00 PM", "06/27/2013 16:00 PM", NumOfTestCases, 10));
            Assert.AreEqual(true, tripProcessor.RunSimulations("06/01/2013 00:00 PM", "06/27/2013 16:00 PM", NumOfTestCases, 20));
            Assert.AreEqual(true, tripProcessor.RunSimulations("06/01/2013 00:00 PM", "06/27/2013 16:00 PM", NumOfTestCases, 30));
        }
    }
}
