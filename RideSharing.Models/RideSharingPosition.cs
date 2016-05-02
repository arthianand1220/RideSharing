using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Models
{
    public class RideSharingPosition
    {
        public Double Latitude { get; set; }

        public Double Longitude { get; set; }

        public RideSharingPosition()
        {

        }
        public RideSharingPosition(double lat, double lon)
        {
            Latitude = lat;
            Longitude = lon;
        }
    }
}
