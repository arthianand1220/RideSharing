using Newtonsoft.Json;
using RideSharing.BL;
using RideSharing.Contract;
using RideSharing.DAL;
using RideSharingWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RideSharingWeb.Controllers
{
    public class ResultsController : Controller
    {
        ITripProcessor tripProcessor;
        
        public ResultsController()
        {
            RideDetailsRepository rideDetailsRepo = new RideDetailsRepository();
            TripDetailsRepository tripDetailsRepo = new TripDetailsRepository();
            IRideProcessor rideProcessor = new RideProcessor(rideDetailsRepo);
            tripProcessor = new TripProcessor(tripDetailsRepo, rideProcessor, "http://192.168.0.114:5000/");
        }

        // GET: Results
        public ActionResult Index()
        {
            ResultsViewModel resultViewModel = new ResultsViewModel();
            resultViewModel.SimulationIds = tripProcessor.GetSimulationIds();
            foreach(var sId in resultViewModel.SimulationIds)
            {
                var result = tripProcessor.GetTrips(sId);
                resultViewModel.Trips.Add(sId, result);
            }

            return View(resultViewModel);
        }
    }
}