using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COBIeManager.Shared.Utils
{
    public static class UnitHelper
    {
        public static double RoundToNearest50mm(double internalLength)
        {
            // Convert to mm
            double mm = UnitUtils.ConvertFromInternalUnits(internalLength, UnitTypeId.Millimeters);
            // Round to nearest 50
            double roundedMm = Math.Round(mm / 50.0) * 50.0;
            // Back to feet
            return UnitUtils.ConvertToInternalUnits(roundedMm, UnitTypeId.Millimeters);
        }
    }
}
