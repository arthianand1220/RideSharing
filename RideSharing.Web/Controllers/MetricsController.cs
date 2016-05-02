using RideSharing.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace RideSharingWeb.Controllers
{
    public class MetricsController : Controller
    {
        MetricsRepository mRepository;

        public MetricsController()
        {
            mRepository = new MetricsRepository();
        }

        // GET: Metrics
        public ActionResult Index()
        {
            return View();
        }

        public string GetProcessingTimeVSPoolSize()
        {
            return mRepository.GetProcessingTimeVSPoolSize();
        }
    }
}