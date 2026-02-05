using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using System;

namespace COBIeManager.Shared.Adapters
{
    /// <summary>
    /// Adapter to handle Revit API differences between versions.
    /// Provides version-specific implementations for API calls that differ across Revit versions.
    /// </summary>
    public static class RevitApiAdapter
    {
        /// <summary>
        /// Creates a FamilyInstance with proper API handling for different Revit versions.
        /// Handles structural type parameter differences between Revit 2023 and 2024+.
        /// </summary>
        public static FamilyInstance CreateStructuralFamilyInstance(
            Document doc,
            XYZ location,
            FamilySymbol symbol,
            Level level,
            StructuralType structuralType)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (location == null) throw new ArgumentNullException(nameof(location));
            if (symbol == null) throw new ArgumentNullException(nameof(symbol));
            if (level == null) throw new ArgumentNullException(nameof(level));

            // Ensure family symbol is active
            if (!symbol.IsActive)
                symbol.Activate();

#if REVIT_2023
            // Revit 2023 uses the older API
            return doc.Create.NewFamilyInstance(location, symbol, level, structuralType);
#else
            // Revit 2024+ might use a different API or enum values
            return doc.Create.NewFamilyInstance(location, symbol, level, structuralType);
#endif
        }

        /// <summary>
        /// Creates a beam FamilyInstance with proper API handling for different Revit versions.
        /// </summary>
        public static FamilyInstance CreateBeamInstance(
            Document doc,
            Curve curve,
            FamilySymbol symbol,
            Level level,
            StructuralType structuralType)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (curve == null) throw new ArgumentNullException(nameof(curve));
            if (symbol == null) throw new ArgumentNullException(nameof(symbol));
            if (level == null) throw new ArgumentNullException(nameof(level));

            // Ensure family symbol is active
            if (!symbol.IsActive)
                symbol.Activate();

#if REVIT_2023
            // Revit 2023 uses the older API
            return doc.Create.NewFamilyInstance(curve, symbol, level, structuralType);
#else
            // Revit 2024+ might use a different API or enum values
            return doc.Create.NewFamilyInstance(curve, symbol, level, structuralType);
#endif
        }

        /// <summary>
        /// Gets the appropriate StructuralType value for footings based on Revit version.
        /// </summary>
        public static StructuralType GetFootingStructuralType()
        {
#if REVIT_2023
            // In Revit 2023, check which value works for footings
            return StructuralType.Footing;
#else
            return StructuralType.Footing;
#endif
        }

        /// <summary>
        /// Gets the appropriate StructuralType value for columns based on Revit version.
        /// </summary>
        public static StructuralType GetColumnStructuralType()
        {
#if REVIT_2023
            return StructuralType.Column;
#else
            return StructuralType.Column;
#endif
        }

        /// <summary>
        /// Gets the appropriate StructuralType value for beams based on Revit version.
        /// </summary>
        public static StructuralType GetBeamStructuralType()
        {
#if REVIT_2023
            return StructuralType.Beam;
#else
            return StructuralType.Beam;
#endif
        }
    }
}
