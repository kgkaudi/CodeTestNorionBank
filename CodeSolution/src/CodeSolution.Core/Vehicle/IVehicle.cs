using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace CodeSolution.Core.Vehicle;

public interface IVehicle
{
    VehicleType Type { get; }
}