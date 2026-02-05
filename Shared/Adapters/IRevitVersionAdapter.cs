using Autodesk.Revit.DB;

namespace COBIeManager.Shared.Adapters
{
    /// <summary>
    /// Adapter pattern for handling API differences between Revit versions.
    /// </summary>
    public interface IRevitVersionAdapter
    {
        /// <summary>
        /// Gets the Revit version this adapter supports.
        /// </summary>
        int RevitVersion { get; }

        /// <summary>
        /// Gets the geometry from an ImportInstance in a version-compatible way.
        /// </summary>
        GeometryElement GetImportGeometry(ImportInstance importInstance, bool computeReferences = true);

        /// <summary>
        /// Gets element parameters in a version-compatible way.
        /// </summary>
        Parameter GetParameter(Element element, BuiltInParameter builtInParameter);

        /// <summary>
        /// Sets element parameters in a version-compatible way.
        /// </summary>
        void SetParameter(Element element, BuiltInParameter builtInParameter, object value);
    }
}
