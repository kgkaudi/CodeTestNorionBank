using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeSolution.Core.Vehicle;

public sealed class Car : IVehicle
{
    public VehicleType Type => VehicleType.Car;
}