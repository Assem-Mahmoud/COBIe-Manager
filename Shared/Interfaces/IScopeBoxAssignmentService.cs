using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Models;
using System;
using System.Collections.Generic;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service interface for scope box-based parameter filling.
    /// Handles finding elements within scope box bounds and assigning parameters.
    /// </summary>
    public interface IScopeBoxAssignmentService
    {
        /// <summary>
        /// Gets all available scope boxes in the document
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <returns>List of scope boxes sorted by name</returns>
        IList<Element> GetScopeBoxes(Document document);

        /// <summary>
        /// Gets all available scope boxes in the specified document (for linked document support)
        /// </summary>
        /// <param name="document">The Revit document to get scope boxes from</param>
        /// <returns>List of scope boxes sorted by name</returns>
        IList<Element> GetScopeBoxesFromDocument(Document document);

        /// <summary>
        /// Gets the bounding box of a scope box
        /// </summary>
        /// <param name="scopeBox">The scope box element</param>
        /// <returns>The bounding box in model coordinates</returns>
        BoundingBoxXYZ GetScopeBoxBoundingBox(Element scopeBox);

        /// <summary>
        /// Checks if an element is contained within a scope box's bounding box
        /// </summary>
        /// <param name="element">The element to check</param>
        /// <param name="scopeBoxBoundingBox">The scope box bounding box</param>
        /// <param name="tolerance">Optional tolerance in feet to extend the bounds</param>
        /// <returns>True if the element is completely inside the bounding box</returns>
        bool IsElementInScopeBox(Element element, BoundingBoxXYZ scopeBoxBoundingBox, double tolerance = 0.0);

        /// <summary>
        /// Finds all elements within the selected scope box bounds
        /// </summary>
        /// <param name="targetDocument">The document containing elements to fill</param>
        /// <param name="sourceDocument">The document containing scope boxes (for linked doc support)</param>
        /// <param name="config">The fill configuration containing scope box settings</param>
        /// <returns>Dictionary mapping elements to their assigned value</returns>
        IDictionary<Element, string> FindElementsInScopeBoxes(Document targetDocument, Document sourceDocument, FillConfiguration config, Transform coordinateTransform);

        /// <summary>
        /// Finds all elements within the selected scope box bounds (legacy method for backward compatibility)
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <param name="config">The fill configuration containing scope box settings</param>
        /// <returns>Dictionary mapping elements to their assigned value</returns>
        IDictionary<Element, string> FindElementsInScopeBox(Document document, FillConfiguration config);

        /// <summary>
        /// Preview the scope box fill operation
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <param name="config">The fill configuration</param>
        /// <param name="progressAction">Optional progress callback</param>
        /// <returns>Summary of what would be filled</returns>
        ScopeBoxFillSummary PreviewFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null);

        /// <summary>
        /// Execute the scope box fill operation
        /// </summary>
        /// <param name="document">The Revit document</param>
        /// <param name="config">The fill configuration</param>
        /// <param name="progressAction">Optional progress callback</param>
        /// <returns>Summary of what was filled</returns>
        ScopeBoxFillSummary ExecuteFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null);
    }

    /// <summary>
    /// Summary of scope box fill operation results
    /// </summary>
    public class ScopeBoxFillSummary
    {
        /// <summary>
        /// Number of elements found within the scope box
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
        /// Name of the scope box used
        /// </summary>
        public string ScopeBoxName { get; set; }

        /// <summary>
        /// Value that was filled (custom name or scope box name)
        /// </summary>
        public string FillValue { get; set; }

        /// <summary>
        /// Elements not in any scope box that were filled with NotAssignedValue
        /// </summary>
        public int ElementsNotInScopeBox { get; set; }

        /// <summary>
        /// Parameters filled with NotAssignedValue for elements not in any scope box
        /// </summary>
        public int ParametersFilledWithNA { get; set; }

        /// <summary>
        /// Time taken for the operation
        /// </summary>
        public TimeSpan ProcessingDuration { get; set; }
    }
}
