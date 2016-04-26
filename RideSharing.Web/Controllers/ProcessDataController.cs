using GeoJSON.Net.Feature;
using GeoJSON.Net.Geometry;
using Newtonsoft.Json;
using RideSharing.BL;
using RideSharing.Contract;
using RideSharing.DAL;
using RideSharing.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace RideSharingWeb.Controllers
{
    public class ProcessDataController : Controller
    {
        private IRideProcessor rideProcessor;

        public ProcessDataController()
        {
            RideDetailsRepository rideDetailsRepo = new RideDetailsRepository();
            rideProcessor = new RideProcessor(rideDetailsRepo);
        }

        public ActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        public String GetRecords(string startDateTime, string endDateTime)
        {
            if (DateTime.Compare(DateTime.Parse(startDateTime), DateTime.Parse(endDateTime)) < 0)
            {
                List<RideSharingPosition> tempData = rideProcessor.GetRideLocations(startDateTime, endDateTime);
                FeatureCollection returnData = new FeatureCollection();
                foreach (var data in tempData)
                {                    
                    Point p = new Point(new GeographicPosition(data.Latitude, data.Longitude));
                    Feature feature = new Feature(p, null);
                    returnData.Features.Add(feature);
                }
                return JsonConvert.SerializeObject(returnData, Formatting.Indented);
            }
            return "";
        }
    }
}