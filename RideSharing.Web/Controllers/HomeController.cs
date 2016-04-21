using RideSharing.BL;
using RideSharing.Contract;
using RideSharing.DAL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RideSharing.Controllers
{
    public class HomeController : Controller
    {
        private IDataProcessor dataProcessor;

        public HomeController()
        {
            RideDetailsRepository rideDetailsRepo = new RideDetailsRepository();
            dataProcessor = new DataProcessor(rideDetailsRepo); 
        }

        // GET: Home
        public ActionResult Index()
        {
            return View();
        }
        
        [HttpPost]
        public ActionResult Index(int maxWalkingTime, int maxWaitingTime)
        {
            long numberRecords = 0;
            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];

                if (file != null && file.ContentLength > 0)
                {
                    var fileName = Path.GetFileName(file.FileName);
                    var path = Path.Combine(Server.MapPath("~/Content/Data/"), fileName);
                    file.SaveAs(path);
                    numberRecords = dataProcessor.ProcessData(path, maxWaitingTime, maxWalkingTime);
                }
            }

            ViewBag.Message = "Data processed successfully!<br />" + numberRecords + " valid records are imported!";
            return View();
        }
    }
}