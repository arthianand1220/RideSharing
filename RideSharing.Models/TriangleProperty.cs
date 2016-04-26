using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Models
{
    public class TriangleProperty
    {
        public double WalkDistance { get; set; }

        public double DriveDistance { get; set; }

        public double DriveTime { get; set; }

        public RideSharingPosition Position { get; set; }

        public TriangleProperty()
        {
            Position = new RideSharingPosition();
            WalkDistance = 0;
        }
    }
}
