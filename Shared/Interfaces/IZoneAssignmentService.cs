using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Models;
using System;
using System.Collections.Generic;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service interface for zone-based parameter filling.
    /// Handles finding elements within zone bounds and assigning parameters.
    /// Zones are represented by scope boxes in Revit.
    /// </summary>
    public interface IZoneAssignmentService
    {
        /// <summary>
        /// Gets all available zones (scope boxes) in the document
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <returns>List of zones sorted by name</returns>
        IList<Element> GetZones(Document document);

        /// <summary>
        /// Gets all available zones (scope boxes) from the specified document
        /// </summary>
        /// <param name="document">The Revit document to get zones from</param>
        /// <returns>List of zones sorted by name</returns>
        IList<Element> GetZonesFromDocument(Document document);

        /// <summary>
        /// Gets the bounding box of a zone (scope box)
        /// </summary>
        /// <param name="zone">The zone element (scope box)</param>
        /// <returns>The bounding box in model coordinates</returns>
        BoundingBoxXYZ GetZoneBoundingBox(Element zone);

        /// <summary>
        /// Checks if an element is contained within a zone's bounding box
        /// </summary>
        /// <param name="element">The element to check</param>
        /// <param name="zoneBoundingBox">The zone bounding box</param>
        /// <returns>True if the element is completely inside the bounding box</returns>
        bool IsElementInZone(Element element, BoundingBoxXYZ zoneBoundingBox);

        /// <summary>
        /// Finds all elements within the selected zone bounds
        /// </summary>
        /// <param name="targetDocument">Document containing elements to fill parameters on</param>
        /// <param name="sourceDocument">Document containing zones (may be a linked document)</param>
        /// <param name="config">The fill configuration containing zone settings</param>
        /// <returns>Dictionary mapping elements to their assigned value</returns>
        IDictionary<Element, string> FindElementsInZones(Document targetDocument, Document sourceDocument, FillConfiguration config, Transform coordinateTransform);

        /// <summary>
        /// Finds all elements within the selected zone bounds (legacy method for backward compatibility)
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <param name="config">The fill configuration containing zone settings</param>
        /// <returns>Dictionary mapping elements to their assigned value</returns>
        IDictionary<Element, string> FindElementsInZones(Document document, FillConfiguration config);

        /// <summary>
        /// Preview the zone fill operation
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <param name="config">The fill configuration</param>
        /// <param name="progressAction">Optional progress callback</param>
        /// <returns>Summary of what would be filled</returns>
        ZoneFillSummary PreviewFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null);

        /// <summary>
        /// Execute the zone fill operation
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <param name="config">The fill configuration</param>
        /// <param name="progressAction">Optional progress callback</param>
        /// <returns>Summary of what was filled</returns>
        ZoneFillSummary ExecuteFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null);
    }

    /// <summary>
    /// Summary of zone fill operation results
    /// </summary>
    public class ZoneFillSummary
    {
        /// <summary>
        /// Number of elements found within the zone
        /// </summary>
        public int ElementsFound { get; set; }

        /// <summary>
        /// Number of parameters successfully filled
        /// </summary>
        public int ParametersFilled { get; set; }

        /// <summary>
        /// Number of parameters skipped (already had value or read-only)
        /// </summary>
        public int ParametersSkipped { get; set; }

        /// <summary>
        /// Number of errors encountered
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// List of error messages
        /// </summary>
        public IList<string> ErrorMessages { get; set; } = new List<string>();

        /// <summary>
        /// Name of the zone used
        /// </summary>
        public string ZoneName { get; set; }

        /// <summary>
        /// Value that was filled (zone name)
        /// </summary>
        public string FillValue { get; set; }

        /// <summary>
        /// Time taken for the operation
        /// </summary>
        public TimeSpan ProcessingDuration { get; set; }
    }
}
