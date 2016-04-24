using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RideSharing.BL;
using RideSharing.DAL;
using RideSharing.Contract;

namespace RideSharing.Tests
{
    [TestClass]
    public class TripProcessorTests
    {
        [TestMethod]
        public void TestProcessTripsForSmallPoolSize_Valid()
        {
            RideDetailsRepository rideDetailsrepo = new RideDetailsRepository();
            IRideProcessor rideProcessor = new RideProcessor(rideDetailsrepo);
            TripProcessor tripProcessor = new TripProcessor(rideDetailsrepo, rideProcessor, "http://192.168.0.113:5000/");

            Assert.AreEqual(true, tripProcessor.ProcessTrips(20160402121212, "02/12/2013 15:30 PM", "02/12/2013 15:35 PM"));            
        }

        [TestMethod]
        public void TestProcessTripsForLargePoolSize_Valid()
        {
            RideDetailsRepository rideDetailsrepo = new RideDetailsRepository();
            IRideProcessor rideProcessor = new RideProcessor(rideDetailsrepo);
            TripProcessor tripProcessor = new TripProcessor(rideDetailsrepo, rideProcessor, "http://192.168.0.113:5000/");

            Assert.AreEqual(true, tripProcessor.ProcessTrips(20160402121212, "02/12/2013 16:00 PM", "02/12/2013 16:30 PM"));
        }
    }
}
