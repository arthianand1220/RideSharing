using RideSharing.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RideSharing.Contract
{
    public interface IDataProcessor
    {
        long ProcessData(string FilePath, int MaxWaitingTime, int MaxWalkingTime);
    }
}
