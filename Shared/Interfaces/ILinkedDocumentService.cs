using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using System.Collections.Generic;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for managing linked Revit documents as data sources for parameter filling.
    /// Handles discovery of Revit links, coordinate transformation between documents,
    /// and provides a unified interface for accessing spatial data from either the
    /// current document or a linked model.
    /// </summary>
    public interface ILinkedDocumentService
    {
        /// <summary>
        /// Gets all available linked documents in the host document,
        /// including a special "Current Document" option as the first item.
        /// </summary>
        /// <param name="hostDocument">The host Revit document containing the links</param>
        /// <returns>List of linked document items with "Current Document" as first item</returns>
        IList<LinkedDocumentItem> GetLinkedDocuments(Document hostDocument);

        /// <summary>
        /// Gets the linked Document from a RevitLinkInstance
        /// </summary>
        /// <param name="linkInstance">The Revit link instance</param>
        /// <returns>The linked document, or null if not available</returns>
        Document GetLinkedDocument(RevitLinkInstance linkInstance);

        /// <summary>
        /// Gets the transform from the host document to the linked document
        /// </summary>
        /// <param name="linkInstance">The Revit link instance</param>
        /// <returns>The transform representing the link's position in the host model</returns>
        Transform GetLinkTransform(RevitLinkInstance linkInstance);

        /// <summary>
        /// Gets the inverse transform for converting points from host to linked document space
        /// </summary>
        /// <param name="linkInstance">The Revit link instance</param>
        /// <returns>The inverse transform</returns>
        Transform GetInverseLinkTransform(RevitLinkInstance linkInstance);

        /// <summary>
        /// Transforms a point from host document coordinates to linked document coordinates
        /// </summary>
        /// <param name="point">The point in host document coordinates</param>
        /// <param name="transform">The inverse link transform</param>
        /// <returns>The point in linked document coordinates</returns>
        XYZ TransformPointToLinkedDocument(XYZ point, Transform transform);

        /// <summary>
        /// Transforms a bounding box from host document coordinates to linked document coordinates
        /// </summary>
        /// <param name="bbox">The bounding box in host document coordinates</param>
        /// <param name="transform">The inverse link transform</param>
        /// <returns>The bounding box in linked document coordinates</returns>
        BoundingBoxXYZ TransformBoundingBoxToLinkedDocument(BoundingBoxXYZ bbox, Transform transform);

        /// <summary>
        /// Checks if a linked document item represents the current document
        /// </summary>
        /// <param name="linkedDocItem">The linked document item to check</param>
        /// <returns>True if this is the current document option</returns>
        bool IsCurrentDocument(LinkedDocumentItem linkedDocItem);

        /// <summary>
        /// Gets the appropriate document for spatial queries based on the selected linked document
        /// </summary>
        /// <param name="hostDocument">The active/host document</param>
        /// <param name="selectedLinkedDoc">The selected linked document item (may be Current Document)</param>
        /// <returns>The document to use for spatial queries (host or linked)</returns>
        Document GetSourceDocument(Document hostDocument, LinkedDocumentItem selectedLinkedDoc);
    }
}
