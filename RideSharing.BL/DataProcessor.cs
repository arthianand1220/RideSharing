using Microsoft.SqlServer.Types;
using RideSharing.Contract;
using RideSharing.DAL;
using RideSharing.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RideSharing.BL
{
    public class DataProcessor : IDataProcessor
    {
        private RideDetailsRepository rdRepo;

        public DataProcessor(RideDetailsRepository RDRepo)
        {
            rdRepo = RDRepo;
        }

        public long ProcessData(string FilePath, int MaxWaitingTime, int MaxWalkingTime)
        {
            ConcurrentBag<RideDetailsDBRecord> tempRideDetails = new ConcurrentBag<RideDetailsDBRecord>();
            Random random = new Random();
            string TableName = "";

            Parallel.ForEach(File.ReadLines(FilePath), (line, _, lineNumber) =>
            {
                var tempLines = line.Split(',');
                try
                {
                    RideDetailsDBRecord dm = new RideDetailsDBRecord();
                    dm.Id = Convert.ToInt64(lineNumber);
                    var sourceLat = Convert.ToDouble(tempLines[11]);
                    var sourceLong = Convert.ToDouble(tempLines[10]);
                    var destLat = Convert.ToDouble(tempLines[13]);
                    var destLong = Convert.ToDouble(tempLines[12]);

                    if ((sourceLong >= -73.825722 && sourceLat >= 40.642354) &&
                        (sourceLong <= -73.752251 && sourceLat <= 40.67491) &&
                        !((destLong >= -73.825722 && destLat >= 40.642354) &&
                        (destLong <= -73.752251 && destLat <= 40.67491)))
                    {
                        dm.Destination = SqlGeography.Point(destLat, destLong, 4326);
                        dm.PickupDateTime = DateTime.ParseExact(tempLines[5], "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        dm.DropoffDateTime = DateTime.ParseExact(tempLines[6], "yyyy-MM-dd HH:mm:ss",
                                       System.Globalization.CultureInfo.InvariantCulture);
                        if (String.IsNullOrEmpty(TableName))
                            TableName = "RideDetails" + dm.PickupDateTime.Year + dm.PickupDateTime.Month.ToString("00");
                        dm.PassengerCount = Convert.ToInt32(tempLines[7]);
                        dm.Duration = Convert.ToDouble(tempLines[8]);
                        dm.Distance = Convert.ToDouble(tempLines[9]);
                        dm.WaitTime = random.Next(0, MaxWaitingTime);
                        dm.WalkTime = random.Next(0, MaxWalkingTime);
                        string sec = GetSector(destLat, destLong);
                        dm.Sector = string.IsNullOrEmpty(sec) ? "Sector5" : sec;
                        tempRideDetails.Add(dm);
                    }
                }
                catch (Exception)
                {
                }
            });

            return rdRepo.StoreRideDetails(tempRideDetails.OrderBy(o => o.PickupDateTime).ToList(), TableName);
        }
        public string GetSector(double latitude, double longitude)
        {
            string sectorName = string.Empty;
            try
            {
                Dictionary<string, List<RideSharingPosition>> sectorInfo = new Dictionary<string, List<RideSharingPosition>>();
                List<RideSharingPosition> Sector2 = new List<RideSharingPosition>();
                Sector2.Add(new RideSharingPosition(40.669181, -73.994637));
                Sector2.Add(new RideSharingPosition(40.704327, -74.019699));
                Sector2.Add(new RideSharingPosition(40.755060, -74.011116));
                Sector2.Add(new RideSharingPosition(40.788860, -73.985023));
                Sector2.Add(new RideSharingPosition(40.833294, -73.952408));
                Sector2.Add(new RideSharingPosition(40.849134, -73.947258));
                Sector2.Add(new RideSharingPosition(40.878997, -73.927002));
                Sector2.Add(new RideSharingPosition(40.900535, -73.916531));
                Sector2.Add(new RideSharingPosition(40.925187, -73.910179));
                Sector2.Add(new RideSharingPosition(40.937637, -73.905029));
                Sector2.Add(new RideSharingPosition(40.947751, -73.877048));
                Sector2.Add(new RideSharingPosition(40.939193, -73.850441));
                Sector2.Add(new RideSharingPosition(40.932450, -73.826752));
                Sector2.Add(new RideSharingPosition(40.921814, -73.842030));
                Sector2.Add(new RideSharingPosition(40.894434, -73.860312));
                Sector2.Add(new RideSharingPosition(40.864459, -73.870525));
                Sector2.Add(new RideSharingPosition(40.841866, -73.870525));
                Sector2.Add(new RideSharingPosition(40.832187, -73.871641));
                Sector2.Add(new RideSharingPosition(40.824361, -73.870654));
                Sector2.Add(new RideSharingPosition(40.821357, -73.882349));
                Sector2.Add(new RideSharingPosition(40.820441, -73.887681));
                Sector2.Add(new RideSharingPosition(40.811730, -73.897133));
                Sector2.Add(new RideSharingPosition(40.801948, -73.913612));
                Sector2.Add(new RideSharingPosition(40.787314, -73.926487));
                Sector2.Add(new RideSharingPosition(40.772853, -73.918333));
                Sector2.Add(new RideSharingPosition(40.767447, -73.936400));
                Sector2.Add(new RideSharingPosition(40.749403, -73.952129));
                Sector2.Add(new RideSharingPosition(40.741035, -73.958105));
                Sector2.Add(new RideSharingPosition(40.733861, -73.960063));
                Sector2.Add(new RideSharingPosition(40.725202, -73.957265));
                Sector2.Add(new RideSharingPosition(40.717622, -73.964106));
                Sector2.Add(new RideSharingPosition(40.705635, -73.967527));
                Sector2.Add(new RideSharingPosition(40.698341, -73.975760));
                Sector2.Add(new RideSharingPosition(40.693133, -73.978847));
                Sector2.Add(new RideSharingPosition(40.696126, -73.983308));
                Sector2.Add(new RideSharingPosition(40.698924, -73.992406));
                Sector2.Add(new RideSharingPosition(40.685484, -73.997641));
                Sector2.Add(new RideSharingPosition(40.669702, -73.995323));
                sectorInfo.Add("Sector2", Sector2);

                List<RideSharingPosition> Sector1 = new List<RideSharingPosition>();
                Sector1.Add(new RideSharingPosition(40.633236, -74.202347));
                Sector1.Add(new RideSharingPosition(40.646913, -74.187241));
                Sector1.Add(new RideSharingPosition(40.643917, -74.163895));
                Sector1.Add(new RideSharingPosition(40.640140, -74.151878));
                Sector1.Add(new RideSharingPosition(40.648866, -74.082184));
                Sector1.Add(new RideSharingPosition(40.642994, -74.063988));
                Sector1.Add(new RideSharingPosition(40.637617, -74.073172));
                Sector1.Add(new RideSharingPosition(40.624946, -74.072742));
                Sector1.Add(new RideSharingPosition(40.608619, -74.055147));
                Sector1.Add(new RideSharingPosition(40.614527, -74.040856));
                Sector1.Add(new RideSharingPosition(40.629468, -74.048817));
                Sector1.Add(new RideSharingPosition(40.645277, -74.037005));
                Sector1.Add(new RideSharingPosition(40.660474, -74.020112));
                Sector1.Add(new RideSharingPosition(40.669421, -73.997726));
                Sector1.Add(new RideSharingPosition(40.660420, -73.976097));
                Sector1.Add(new RideSharingPosition(40.624660, -73.965969));
                Sector1.Add(new RideSharingPosition(40.595832, -73.960218));
                Sector1.Add(new RideSharingPosition(40.572029, -73.959403));
                Sector1.Add(new RideSharingPosition(40.567949, -74.002254));
                Sector1.Add(new RideSharingPosition(40.576861, -74.014753));
                Sector1.Add(new RideSharingPosition(40.592789, -74.002463));
                Sector1.Add(new RideSharingPosition(40.602837, -74.029277));
                Sector1.Add(new RideSharingPosition(40.600040, -74.054357));
                Sector1.Add(new RideSharingPosition(40.584731, -74.067078));
                Sector1.Add(new RideSharingPosition(40.531024, -74.134369));
                Sector1.Add(new RideSharingPosition(40.516147, -74.186211));
                Sector1.Add(new RideSharingPosition(40.506490, -74.207840));
                Sector1.Add(new RideSharingPosition(40.496570, -74.233589));
                Sector1.Add(new RideSharingPosition(40.497092, -74.249039));
                Sector1.Add(new RideSharingPosition(40.501787, -74.264488));
                Sector1.Add(new RideSharingPosition(40.515881, -74.248867));
                Sector1.Add(new RideSharingPosition(40.533889, -74.244490));
                Sector1.Add(new RideSharingPosition(40.545762, -74.248137));
                Sector1.Add(new RideSharingPosition(40.557635, -74.233932));
                Sector1.Add(new RideSharingPosition(40.557635, -74.218140));
                Sector1.Add(new RideSharingPosition(40.557635, -74.214020));
                Sector1.Add(new RideSharingPosition(40.561808, -74.211273));
                Sector1.Add(new RideSharingPosition(40.571719, -74.214706));
                Sector1.Add(new RideSharingPosition(40.588925, -74.203033));
                Sector1.Add(new RideSharingPosition(40.602221, -74.197197));
                Sector1.Add(new RideSharingPosition(40.612910, -74.201660));
                Sector1.Add(new RideSharingPosition(40.635841, -74.205093));
                Sector1.Add(new RideSharingPosition(40.625419, -74.203720));
                sectorInfo.Add("Sector1", Sector1);

                List<RideSharingPosition> Sector3 = new List<RideSharingPosition>();
                Sector3.Add(new RideSharingPosition(40.670288, -73.995355));
                Sector3.Add(new RideSharingPosition(40.673349, -73.995801));
                Sector3.Add(new RideSharingPosition(40.675776, -73.996171));
                Sector3.Add(new RideSharingPosition(40.680612, -73.996868));
                Sector3.Add(new RideSharingPosition(40.683041, -73.997249));
                Sector3.Add(new RideSharingPosition(40.685510, -73.997587));
                Sector3.Add(new RideSharingPosition(40.686166, -73.997356));
                Sector3.Add(new RideSharingPosition(40.687286, -73.996879));
                Sector3.Add(new RideSharingPosition(40.689249, -73.996117));
                Sector3.Add(new RideSharingPosition(40.693419, -73.994551));
                Sector3.Add(new RideSharingPosition(40.696185, -73.993451));
                Sector3.Add(new RideSharingPosition(40.698893, -73.992373));
                Sector3.Add(new RideSharingPosition(40.696136, -73.983355));
                Sector3.Add(new RideSharingPosition(40.695428, -73.982288));
                Sector3.Add(new RideSharingPosition(40.693110, -73.978812));
                Sector3.Add(new RideSharingPosition(40.694183, -73.978173));
                Sector3.Add(new RideSharingPosition(40.695704, -73.977288));
                Sector3.Add(new RideSharingPosition(40.698348, -73.975679));
                Sector3.Add(new RideSharingPosition(40.699088, -73.974874));
                Sector3.Add(new RideSharingPosition(40.702317, -73.971194));
                Sector3.Add(new RideSharingPosition(40.703981, -73.969375));
                Sector3.Add(new RideSharingPosition(40.705628, -73.967471));
                Sector3.Add(new RideSharingPosition(40.709108, -73.966484));
                Sector3.Add(new RideSharingPosition(40.712897, -73.965433));
                Sector3.Add(new RideSharingPosition(40.717621, -73.964049));
                Sector3.Add(new RideSharingPosition(40.718922, -73.962890));
                Sector3.Add(new RideSharingPosition(40.722105, -73.960004));
                Sector3.Add(new RideSharingPosition(40.725199, -73.957236));
                Sector3.Add(new RideSharingPosition(40.729346, -73.958566));
                Sector3.Add(new RideSharingPosition(40.733842, -73.960025));
                Sector3.Add(new RideSharingPosition(40.735969, -73.959478));
                Sector3.Add(new RideSharingPosition(40.738405, -73.958759));
                Sector3.Add(new RideSharingPosition(40.740965, -73.953524));
                Sector3.Add(new RideSharingPosition(40.740535, -73.947537));
                Sector3.Add(new RideSharingPosition(40.739738, -73.943809));
                Sector3.Add(new RideSharingPosition(40.737600, -73.940188));
                Sector3.Add(new RideSharingPosition(40.737452, -73.936103));
                Sector3.Add(new RideSharingPosition(40.737238, -73.930827));
                Sector3.Add(new RideSharingPosition(40.736828, -73.920189));
                Sector3.Add(new RideSharingPosition(40.735704, -73.916785));
                Sector3.Add(new RideSharingPosition(40.734653, -73.913757));
                Sector3.Add(new RideSharingPosition(40.732665, -73.907830));
                Sector3.Add(new RideSharingPosition(40.730844, -73.902481));
                Sector3.Add(new RideSharingPosition(40.729015, -73.897144));
                Sector3.Add(new RideSharingPosition(40.729578, -73.895172));
                Sector3.Add(new RideSharingPosition(40.730214, -73.892846));
                Sector3.Add(new RideSharingPosition(40.731177, -73.889503));
                Sector3.Add(new RideSharingPosition(40.733145, -73.882432));
                Sector3.Add(new RideSharingPosition(40.737128, -73.868396));
                Sector3.Add(new RideSharingPosition(40.745127, -73.839895));
                Sector3.Add(new RideSharingPosition(40.741586, -73.841024));
                Sector3.Add(new RideSharingPosition(40.738110, -73.842131));
                Sector3.Add(new RideSharingPosition(40.733773, -73.842251));
                Sector3.Add(new RideSharingPosition(40.732240, -73.842628));
                Sector3.Add(new RideSharingPosition(40.727386, -73.843703));
                Sector3.Add(new RideSharingPosition(40.724182, -73.839884));
                Sector3.Add(new RideSharingPosition(40.720978, -73.836097));
                Sector3.Add(new RideSharingPosition(40.717916, -73.833361));
                Sector3.Add(new RideSharingPosition(40.712448, -73.828297));
                Sector3.Add(new RideSharingPosition(40.705869, -73.821259));
                Sector3.Add(new RideSharingPosition(40.698763, -73.817310));
                Sector3.Add(new RideSharingPosition(40.690475, -73.812317));
                Sector3.Add(new RideSharingPosition(40.678049, -73.805358));
                Sector3.Add(new RideSharingPosition(40.676232, -73.805350));
                Sector3.Add(new RideSharingPosition(40.673011, -73.805628));
                Sector3.Add(new RideSharingPosition(40.671959, -73.805730));
                Sector3.Add(new RideSharingPosition(40.670770, -73.805834));
                Sector3.Add(new RideSharingPosition(40.668716, -73.806015));
                Sector3.Add(new RideSharingPosition(40.668960, -73.808977));
                Sector3.Add(new RideSharingPosition(40.669296, -73.813031));
                Sector3.Add(new RideSharingPosition(40.669695, -73.817866));
                Sector3.Add(new RideSharingPosition(40.669914, -73.820482));
                Sector3.Add(new RideSharingPosition(40.670107, -73.822811));
                Sector3.Add(new RideSharingPosition(40.670335, -73.825586));
                Sector3.Add(new RideSharingPosition(40.670647, -73.829308));
                Sector3.Add(new RideSharingPosition(40.670877, -73.832223));
                Sector3.Add(new RideSharingPosition(40.667853, -73.839905));
                Sector3.Add(new RideSharingPosition(40.665745, -73.845205));
                Sector3.Add(new RideSharingPosition(40.663450, -73.851128));
                Sector3.Add(new RideSharingPosition(40.658762, -73.856599));
                Sector3.Add(new RideSharingPosition(40.655246, -73.862200));
                Sector3.Add(new RideSharingPosition(40.648346, -73.873272));
                Sector3.Add(new RideSharingPosition(40.639818, -73.878477));
                Sector3.Add(new RideSharingPosition(40.634678, -73.882138));
                Sector3.Add(new RideSharingPosition(40.632473, -73.884996));
                Sector3.Add(new RideSharingPosition(40.629877, -73.888397));
                Sector3.Add(new RideSharingPosition(40.629370, -73.889063));
                Sector3.Add(new RideSharingPosition(40.629226, -73.889306));
                Sector3.Add(new RideSharingPosition(40.628671, -73.890230));
                Sector3.Add(new RideSharingPosition(40.627551, -73.892125));
                Sector3.Add(new RideSharingPosition(40.626401, -73.894047));
                Sector3.Add(new RideSharingPosition(40.625222, -73.895974));
                Sector3.Add(new RideSharingPosition(40.623614, -73.896999));
                Sector3.Add(new RideSharingPosition(40.622652, -73.897618));
                Sector3.Add(new RideSharingPosition(40.621673, -73.898211));
                Sector3.Add(new RideSharingPosition(40.617681, -73.898181));
                Sector3.Add(new RideSharingPosition(40.612645, -73.898271));
                Sector3.Add(new RideSharingPosition(40.609481, -73.898550));
                Sector3.Add(new RideSharingPosition(40.606345, -73.899453));
                Sector3.Add(new RideSharingPosition(40.601865, -73.901086));
                Sector3.Add(new RideSharingPosition(40.600203, -73.904783));
                Sector3.Add(new RideSharingPosition(40.596489, -73.909342));
                Sector3.Add(new RideSharingPosition(40.594556, -73.909403));
                Sector3.Add(new RideSharingPosition(40.592053, -73.909829));
                Sector3.Add(new RideSharingPosition(40.588106, -73.910552));
                Sector3.Add(new RideSharingPosition(40.586436, -73.912254));
                Sector3.Add(new RideSharingPosition(40.585312, -73.913986));
                Sector3.Add(new RideSharingPosition(40.583913, -73.912472));
                Sector3.Add(new RideSharingPosition(40.583069, -73.911160));
                Sector3.Add(new RideSharingPosition(40.581383, -73.911111));
                Sector3.Add(new RideSharingPosition(40.581008, -73.913760));
                Sector3.Add(new RideSharingPosition(40.582344, -73.920602));
                Sector3.Add(new RideSharingPosition(40.582801, -73.923472));
                Sector3.Add(new RideSharingPosition(40.583061, -73.925436));
                Sector3.Add(new RideSharingPosition(40.582540, -73.928161));
                Sector3.Add(new RideSharingPosition(40.580975, -73.931894));
                Sector3.Add(new RideSharingPosition(40.575630, -73.930950));
                Sector3.Add(new RideSharingPosition(40.575153, -73.935230));
                Sector3.Add(new RideSharingPosition(40.575198, -73.937792));
                Sector3.Add(new RideSharingPosition(40.574897, -73.942060));
                Sector3.Add(new RideSharingPosition(40.574946, -73.944243));
                Sector3.Add(new RideSharingPosition(40.574522, -73.947237));
                Sector3.Add(new RideSharingPosition(40.573805, -73.954768));
                Sector3.Add(new RideSharingPosition(40.573674, -73.962965));
                Sector3.Add(new RideSharingPosition(40.572886, -73.970084));
                Sector3.Add(new RideSharingPosition(40.577574, -73.968533));
                Sector3.Add(new RideSharingPosition(40.584472, -73.967664));
                Sector3.Add(new RideSharingPosition(40.593575, -73.965240));
                Sector3.Add(new RideSharingPosition(40.618461, -73.969746));
                Sector3.Add(new RideSharingPosition(40.632335, -73.972481));
                Sector3.Add(new RideSharingPosition(40.647154, -73.975389));
                Sector3.Add(new RideSharingPosition(40.653735, -73.978286));
                Sector3.Add(new RideSharingPosition(40.656099, -73.982341));
                Sector3.Add(new RideSharingPosition(40.659765, -73.987727));
                Sector3.Add(new RideSharingPosition(40.670288, -73.995398));
                sectorInfo.Add("Sector3", Sector3);

                List<RideSharingPosition> Sector5 = new List<RideSharingPosition>();
                Sector5.Add(new RideSharingPosition(40.745189, -73.839798));
                Sector5.Add(new RideSharingPosition(40.756098, -73.839321));
                Sector5.Add(new RideSharingPosition(40.761321, -73.839109));
                Sector5.Add(new RideSharingPosition(40.764188, -73.838976));
                Sector5.Add(new RideSharingPosition(40.765594, -73.838947));
                Sector5.Add(new RideSharingPosition(40.766483, -73.838884));
                Sector5.Add(new RideSharingPosition(40.767576, -73.838865));
                Sector5.Add(new RideSharingPosition(40.769949, -73.838764));
                Sector5.Add(new RideSharingPosition(40.770133, -73.838762));
                Sector5.Add(new RideSharingPosition(40.770389, -73.838584));
                Sector5.Add(new RideSharingPosition(40.770838, -73.838259));
                Sector5.Add(new RideSharingPosition(40.771637, -73.837682));
                Sector5.Add(new RideSharingPosition(40.773343, -73.836468));
                Sector5.Add(new RideSharingPosition(40.774630, -73.835547));
                Sector5.Add(new RideSharingPosition(40.775513, -73.834914));
                Sector5.Add(new RideSharingPosition(40.776098, -73.834493));
                Sector5.Add(new RideSharingPosition(40.776434, -73.834257));
                Sector5.Add(new RideSharingPosition(40.776842, -73.833971));
                Sector5.Add(new RideSharingPosition(40.779481, -73.832082));
                Sector5.Add(new RideSharingPosition(40.780814, -73.831121));
                Sector5.Add(new RideSharingPosition(40.781486, -73.830646));
                Sector5.Add(new RideSharingPosition(40.781837, -73.830394));
                Sector5.Add(new RideSharingPosition(40.782175, -73.830153));
                Sector5.Add(new RideSharingPosition(40.784688, -73.828313));
                Sector5.Add(new RideSharingPosition(40.791966, -73.823141));
                Sector5.Add(new RideSharingPosition(40.789705, -73.814062));
                Sector5.Add(new RideSharingPosition(40.787659, -73.805836));
                Sector5.Add(new RideSharingPosition(40.787251, -73.804288));
                Sector5.Add(new RideSharingPosition(40.789482, -73.798395));
                Sector5.Add(new RideSharingPosition(40.791908, -73.791611));
                Sector5.Add(new RideSharingPosition(40.791667, -73.779373));
                Sector5.Add(new RideSharingPosition(40.772950, -73.768387));
                Sector5.Add(new RideSharingPosition(40.760469, -73.749847));
                Sector5.Add(new RideSharingPosition(40.789923, -73.753109));
                Sector5.Add(new RideSharingPosition(40.810365, -73.742380));
                Sector5.Add(new RideSharingPosition(40.804992, -73.727403));
                Sector5.Add(new RideSharingPosition(40.795462, -73.717918));
                Sector5.Add(new RideSharingPosition(40.806014, -73.683929));
                Sector5.Add(new RideSharingPosition(40.801068, -73.651314));
                Sector5.Add(new RideSharingPosition(40.769102, -73.645477));
                Sector5.Add(new RideSharingPosition(40.758960, -73.654747));
                Sector5.Add(new RideSharingPosition(40.757140, -73.662643));
                Sector5.Add(new RideSharingPosition(40.751417, -73.675003));
                Sector5.Add(new RideSharingPosition(40.737631, -73.682899));
                Sector5.Add(new RideSharingPosition(40.730088, -73.685303));
                Sector5.Add(new RideSharingPosition(40.721242, -73.684959));
                Sector5.Add(new RideSharingPosition(40.708231, -73.683243));
                Sector5.Add(new RideSharingPosition(40.691832, -73.677406));
                Sector5.Add(new RideSharingPosition(40.672696, -73.685131));
                Sector5.Add(new RideSharingPosition(40.659285, -73.709335));
                Sector5.Add(new RideSharingPosition(40.667098, -73.730621));
                Sector5.Add(new RideSharingPosition(40.663452, -73.750534));
                Sector5.Add(new RideSharingPosition(40.649516, -73.758945));
                Sector5.Add(new RideSharingPosition(40.634538, -73.774223));
                Sector5.Add(new RideSharingPosition(40.643031, -73.799758));
                Sector5.Add(new RideSharingPosition(40.655221, -73.809607));
                Sector5.Add(new RideSharingPosition(40.664285, -73.806410));
                Sector5.Add(new RideSharingPosition(40.678244, -73.805165));
                Sector5.Add(new RideSharingPosition(40.706000, -73.821087));
                Sector5.Add(new RideSharingPosition(40.713710, -73.829284));
                Sector5.Add(new RideSharingPosition(40.721209, -73.836300));
                Sector5.Add(new RideSharingPosition(40.727407, -73.843660));
                Sector5.Add(new RideSharingPosition(40.733808, -73.842191));
                Sector5.Add(new RideSharingPosition(40.738129, -73.842094));
                Sector5.Add(new RideSharingPosition(40.745209, -73.839841));
                sectorInfo.Add("Sector5", Sector5);


                List<RideSharingPosition> Sector4 = new List<RideSharingPosition>();
                Sector4.Add(new RideSharingPosition(40.738502, -73.958781));
                Sector4.Add(new RideSharingPosition(40.741031, -73.958091));
                Sector4.Add(new RideSharingPosition(40.742901, -73.956746));
                Sector4.Add(new RideSharingPosition(40.746910, -73.953896));
                Sector4.Add(new RideSharingPosition(40.748857, -73.952490));
                Sector4.Add(new RideSharingPosition(40.749579, -73.951969));
                Sector4.Add(new RideSharingPosition(40.750496, -73.951148));
                Sector4.Add(new RideSharingPosition(40.753984, -73.948099));
                Sector4.Add(new RideSharingPosition(40.767434, -73.936377));
                Sector4.Add(new RideSharingPosition(40.768240, -73.933724));
                Sector4.Add(new RideSharingPosition(40.769058, -73.930938));
                Sector4.Add(new RideSharingPosition(40.770828, -73.925000));
                Sector4.Add(new RideSharingPosition(40.772572, -73.919219));
                Sector4.Add(new RideSharingPosition(40.772815, -73.918303));
                Sector4.Add(new RideSharingPosition(40.773789, -73.918847));
                Sector4.Add(new RideSharingPosition(40.775786, -73.919966));
                Sector4.Add(new RideSharingPosition(40.778276, -73.921367));
                Sector4.Add(new RideSharingPosition(40.781202, -73.923021));
                Sector4.Add(new RideSharingPosition(40.784351, -73.924805));
                Sector4.Add(new RideSharingPosition(40.787326, -73.926453));
                Sector4.Add(new RideSharingPosition(40.787540, -73.926255));
                Sector4.Add(new RideSharingPosition(40.792687, -73.921747));
                Sector4.Add(new RideSharingPosition(40.794440, -73.920203));
                Sector4.Add(new RideSharingPosition(40.797392, -73.917587));
                Sector4.Add(new RideSharingPosition(40.802013, -73.913460));
                Sector4.Add(new RideSharingPosition(40.805065, -73.908306));
                Sector4.Add(new RideSharingPosition(40.807801, -73.903673));
                Sector4.Add(new RideSharingPosition(40.811800, -73.896941));
                Sector4.Add(new RideSharingPosition(40.812242, -73.896519));
                Sector4.Add(new RideSharingPosition(40.813272, -73.895428));
                Sector4.Add(new RideSharingPosition(40.814480, -73.894133));
                Sector4.Add(new RideSharingPosition(40.816402, -73.892037));
                Sector4.Add(new RideSharingPosition(40.817719, -73.890619));
                Sector4.Add(new RideSharingPosition(40.820436, -73.887530));
                Sector4.Add(new RideSharingPosition(40.821062, -73.883306));
                Sector4.Add(new RideSharingPosition(40.822299, -73.878569));
                Sector4.Add(new RideSharingPosition(40.824367, -73.870596));
                Sector4.Add(new RideSharingPosition(40.826310, -73.870853));
                Sector4.Add(new RideSharingPosition(40.829901, -73.871337));
                Sector4.Add(new RideSharingPosition(40.832249, -73.871563));
                Sector4.Add(new RideSharingPosition(40.834629, -73.871317));
                Sector4.Add(new RideSharingPosition(40.843874, -73.870482));
                Sector4.Add(new RideSharingPosition(40.845764, -73.870482));
                Sector4.Add(new RideSharingPosition(40.850957, -73.870472));
                Sector4.Add(new RideSharingPosition(40.860661, -73.870515));
                Sector4.Add(new RideSharingPosition(40.864477, -73.870453));
                Sector4.Add(new RideSharingPosition(40.869234, -73.868857));
                Sector4.Add(new RideSharingPosition(40.877530, -73.866051));
                Sector4.Add(new RideSharingPosition(40.881148, -73.864777));
                Sector4.Add(new RideSharingPosition(40.885643, -73.863257));
                Sector4.Add(new RideSharingPosition(40.891906, -73.861138));
                Sector4.Add(new RideSharingPosition(40.894309, -73.860295));
                Sector4.Add(new RideSharingPosition(40.903833, -73.854003));
                Sector4.Add(new RideSharingPosition(40.913708, -73.847415));
                Sector4.Add(new RideSharingPosition(40.918799, -73.844004));
                Sector4.Add(new RideSharingPosition(40.921791, -73.841976));
                Sector4.Add(new RideSharingPosition(40.923420, -73.839669));
                Sector4.Add(new RideSharingPosition(40.926161, -73.835683));
                Sector4.Add(new RideSharingPosition(40.928448, -73.832427));
                Sector4.Add(new RideSharingPosition(40.932438, -73.826752));
                Sector4.Add(new RideSharingPosition(40.939436, -73.791389));
                Sector4.Add(new RideSharingPosition(40.920490, -73.745213));
                Sector4.Add(new RideSharingPosition(40.840931, -73.761950));
                Sector4.Add(new RideSharingPosition(40.832827, -73.784051));
                Sector4.Add(new RideSharingPosition(40.854851, -73.813705));
                Sector4.Add(new RideSharingPosition(40.820037, -73.793106));
                Sector4.Add(new RideSharingPosition(40.795098, -73.782806));
                Sector4.Add(new RideSharingPosition(40.787299, -73.804350));
                Sector4.Add(new RideSharingPosition(40.791976, -73.823147));
                Sector4.Add(new RideSharingPosition(40.770137, -73.838768));
                Sector4.Add(new RideSharingPosition(40.745176, -73.839798));
                Sector4.Add(new RideSharingPosition(40.729047, -73.897133));
                Sector4.Add(new RideSharingPosition(40.736852, -73.920135));
                Sector4.Add(new RideSharingPosition(40.737633, -73.940198));
                Sector4.Add(new RideSharingPosition(40.739768, -73.943768));
                Sector4.Add(new RideSharingPosition(40.740555, -73.947553));
                Sector4.Add(new RideSharingPosition(40.740974, -73.953513));
                Sector4.Add(new RideSharingPosition(40.738413, -73.958759));
                sectorInfo.Add("Sector4", Sector4);


                List<RideSharingPosition> Sector6 = new List<RideSharingPosition>();
                Sector6.Add(new RideSharingPosition(40.585213, -73.913355));
                Sector6.Add(new RideSharingPosition(40.588159, -73.910415));
                Sector6.Add(new RideSharingPosition(40.594520, -73.909374));
                Sector6.Add(new RideSharingPosition(40.596528, -73.909197));
                Sector6.Add(new RideSharingPosition(40.600269, -73.904302));
                Sector6.Add(new RideSharingPosition(40.601814, -73.900997));
                Sector6.Add(new RideSharingPosition(40.605248, -73.899665));
                Sector6.Add(new RideSharingPosition(40.609504, -73.898458));
                Sector6.Add(new RideSharingPosition(40.613695, -73.898023));
                Sector6.Add(new RideSharingPosition(40.621685, -73.898184));
                Sector6.Add(new RideSharingPosition(40.625245, -73.895985));
                Sector6.Add(new RideSharingPosition(40.629371, -73.889049));
                Sector6.Add(new RideSharingPosition(40.634538, -73.882284));
                Sector6.Add(new RideSharingPosition(40.644958, -73.876147));
                Sector6.Add(new RideSharingPosition(40.648411, -73.873615));
                Sector6.Add(new RideSharingPosition(40.651372, -73.867865));
                Sector6.Add(new RideSharingPosition(40.658323, -73.857265));
                Sector6.Add(new RideSharingPosition(40.663426, -73.851106));
                Sector6.Add(new RideSharingPosition(40.670874, -73.832159));
                Sector6.Add(new RideSharingPosition(40.668713, -73.806029));
                Sector6.Add(new RideSharingPosition(40.664249, -73.806453));
                Sector6.Add(new RideSharingPosition(40.658768, -73.808362));
                Sector6.Add(new RideSharingPosition(40.655174, -73.809591));
                Sector6.Add(new RideSharingPosition(40.651593, -73.806683));
                Sector6.Add(new RideSharingPosition(40.643119, -73.799887));
                Sector6.Add(new RideSharingPosition(40.637982, -73.784738));
                Sector6.Add(new RideSharingPosition(40.635120, -73.776348));
                Sector6.Add(new RideSharingPosition(40.634408, -73.774095));
                Sector6.Add(new RideSharingPosition(40.640889, -73.767571));
                Sector6.Add(new RideSharingPosition(40.643054, -73.752873));
                Sector6.Add(new RideSharingPosition(40.636883, -73.742981));
                Sector6.Add(new RideSharingPosition(40.633365, -73.739891));
                Sector6.Add(new RideSharingPosition(40.626916, -73.740749));
                Sector6.Add(new RideSharingPosition(40.618904, -73.741779));
                Sector6.Add(new RideSharingPosition(40.612649, -73.744612));
                Sector6.Add(new RideSharingPosition(40.605351, -73.741436));
                Sector6.Add(new RideSharingPosition(40.602093, -73.738089));
                Sector6.Add(new RideSharingPosition(40.594338, -73.738046));
                Sector6.Add(new RideSharingPosition(40.594044, -73.744805));
                Sector6.Add(new RideSharingPosition(40.593181, -73.749043));
                Sector6.Add(new RideSharingPosition(40.589971, -73.753624));
                Sector6.Add(new RideSharingPosition(40.590623, -73.770447));
                Sector6.Add(new RideSharingPosition(40.586842, -73.787613));
                Sector6.Add(new RideSharingPosition(40.582736, -73.814135));
                Sector6.Add(new RideSharingPosition(40.574718, -73.839626));
                Sector6.Add(new RideSharingPosition(40.558156, -73.890266));
                Sector6.Add(new RideSharingPosition(40.556330, -73.901553));
                Sector6.Add(new RideSharingPosition(40.554504, -73.908377));
                Sector6.Add(new RideSharingPosition(40.547200, -73.929062));
                Sector6.Add(new RideSharingPosition(40.542896, -73.940992));
                Sector6.Add(new RideSharingPosition(40.548109, -73.940263));
                Sector6.Add(new RideSharingPosition(40.552869, -73.941872));
                Sector6.Add(new RideSharingPosition(40.556749, -73.937699));
                Sector6.Add(new RideSharingPosition(40.557907, -73.932179));
                Sector6.Add(new RideSharingPosition(40.561877, -73.925728));
                Sector6.Add(new RideSharingPosition(40.562428, -73.919756));
                Sector6.Add(new RideSharingPosition(40.565507, -73.911277));
                Sector6.Add(new RideSharingPosition(40.564015, -73.903003));
                Sector6.Add(new RideSharingPosition(40.567442, -73.894918));
                Sector6.Add(new RideSharingPosition(40.569025, -73.893451));
                Sector6.Add(new RideSharingPosition(40.569100, -73.885507));
                Sector6.Add(new RideSharingPosition(40.576309, -73.887629));
                Sector6.Add(new RideSharingPosition(40.576328, -73.895814));
                Sector6.Add(new RideSharingPosition(40.579559, -73.897560));
                Sector6.Add(new RideSharingPosition(40.583182, -73.896903));
                Sector6.Add(new RideSharingPosition(40.587685, -73.899679));
                Sector6.Add(new RideSharingPosition(40.586579, -73.904757));
                Sector6.Add(new RideSharingPosition(40.585244, -73.908584));
                Sector6.Add(new RideSharingPosition(40.584952, -73.912067));
                sectorInfo.Add("Sector6", Sector6);
                foreach (string sector in sectorInfo.Keys)
                {
                    bool result = isPointInPolygon(new RideSharingPosition(latitude, longitude), sectorInfo[sector]);
                    if (result)
                    {
                        return sector;

                    }
                }

            }
            catch (Exception ex)
            {

            }
            return sectorName;
        }

        public bool isPointInPolygon(RideSharingPosition tap, List<RideSharingPosition> vertices)
        {
            int intersectCount = 0;
            for (int j = 0; j < vertices.Count() - 1; j++)
            {
                if (rayCastIntersect(tap, vertices[j], vertices[j + 1]))
                {
                    intersectCount++;
                }
            }

            return ((intersectCount % 2) == 1); // odd = inside, even = outside;
        }

        public bool rayCastIntersect(RideSharingPosition tap, RideSharingPosition vertA, RideSharingPosition vertB)
        {

            double aY = vertA.Latitude;
            double bY = vertB.Latitude;
            double aX = vertA.Longitude;
            double bX = vertB.Longitude;
            double pY = tap.Latitude;
            double pX = tap.Longitude;

            if ((aY > pY && bY > pY) || (aY < pY && bY < pY) || (aX < pX && bX < pX))
            {
                return false; // a and b can't both be above or below pt.y, and a or b must be east of pt.x
            }

            double m = (aY - bY) / (aX - bX);               // Rise over run
            double bee = (-aX) * m + aY;                // y = mx + b
            double x = (pY - bee) / m;                  // algebra is neat!

            return x > pX;
        }
    }
}
