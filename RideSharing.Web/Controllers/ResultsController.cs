using Newtonsoft.Json;
using RideSharing.BL;
using RideSharing.Contract;
using RideSharing.DAL;
using RideSharing.Models;
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
        IRideProcessor rideProcessor;
        ITripProcessor tripProcessor;

        public ResultsController()
        {
            RideDetailsRepository rideDetailsRepo = new RideDetailsRepository();
            TripDetailsRepository tripDetailsRepo = new TripDetailsRepository();
            rideProcessor = new RideProcessor(rideDetailsRepo);
            tripProcessor = new TripProcessor(tripDetailsRepo, rideProcessor, "http://192.168.0.114:5000/", "http://192.168.0.111:5000/");
        }
        
        public ActionResult Index()
        {
            ResultsViewModel resultViewModel = new ResultsViewModel();
            resultViewModel.SimulationIds = tripProcessor.GetSimulationIds();
            return View(resultViewModel);
        }

        public string GetDetailsForSimulationId(string Id)
        {
            SimulationViewModel svm = new SimulationViewModel();
            svm = tripProcessor.GetTrips(Convert.ToInt64(Id));

            return JsonConvert.SerializeObject(svm);
        }
    }
}