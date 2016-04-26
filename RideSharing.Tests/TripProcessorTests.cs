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
        string URL = "http://192.168.0.114:5000/";

        [TestMethod]
        public void TestProcessTripsForSmallPoolSize_Valid()
        {
            Trace.WriteLine("Small Pool Size");

            RideDetailsRepository rideDetailsrepo = new RideDetailsRepository();
            IRideProcessor rideProcessor = new RideProcessor(rideDetailsrepo);
            TripProcessor tripProcessor = new TripProcessor(rideDetailsrepo, rideProcessor, URL);

            Assert.AreEqual(true, tripProcessor.ProcessTrips(20160402121212, "02/4/2013 15:30 PM", "02/4/2013 15:35 PM"));            
        }

        [TestMethod]
        public void TestProcessTripsForMediumPoolSize_Valid()
        {
            Trace.WriteLine("Medium Pool Size");

            RideDetailsRepository rideDetailsrepo = new RideDetailsRepository();
            IRideProcessor rideProcessor = new RideProcessor(rideDetailsrepo);
            TripProcessor tripProcessor = new TripProcessor(rideDetailsrepo, rideProcessor, URL);

            Assert.AreEqual(true, tripProcessor.ProcessTrips(20160402121212, "02/16/2013 1:10 PM", "02/16/2013 1:25 PM"));
        }

        [TestMethod]
        public void TestProcessTripsForLargePoolSize_Valid()
        {
            Trace.WriteLine("Large Pool Size");

            RideDetailsRepository rideDetailsrepo = new RideDetailsRepository();
            IRideProcessor rideProcessor = new RideProcessor(rideDetailsrepo);
            TripProcessor tripProcessor = new TripProcessor(rideDetailsrepo, rideProcessor, URL);

            Assert.AreEqual(true, tripProcessor.ProcessTrips(20160402121212, "02/4/2013 16:00 PM", "02/4/2013 16:20 PM"));
        }

        [TestMethod]
        public void TestProcessTripsForExtraLargePoolSize_Valid()
        {
            Trace.WriteLine("Extra Large Pool Size");

            RideDetailsRepository rideDetailsrepo = new RideDetailsRepository();
            IRideProcessor rideProcessor = new RideProcessor(rideDetailsrepo);
            TripProcessor tripProcessor = new TripProcessor(rideDetailsrepo, rideProcessor, URL);

            Assert.AreEqual(true, tripProcessor.ProcessTrips(20160402121212, "02/4/2013 16:00 PM", "02/4/2013 16:30 PM"));
        }
    }
}
