using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Service for managing linked Revit documents as data sources for parameter filling.
    /// Handles discovery of Revit links, coordinate transformation between documents,
    /// and provides a unified interface for accessing spatial data from either the
    /// current document or a linked model.
    /// </summary>
    public class LinkedDocumentService : ILinkedDocumentService
    {
        private readonly ILogger _logger;

        public LinkedDocumentService(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets all available linked documents in the host document,
        /// including a special "Current Document" option as the first item.
        /// </summary>
        /// <param name="hostDocument">The host Revit document containing the links</param>
        /// <returns>List of linked document items with "Current Document" as first item</returns>
        public IList<LinkedDocumentItem> GetLinkedDocuments(Document hostDocument)
        {
            if (hostDocument == null)
                throw new ArgumentNullException(nameof(hostDocument));

            var result = new List<LinkedDocumentItem>();

            try
            {
                // Always add "Current Document" as the first option
                var currentDocItem = LinkedDocumentItem.CreateCurrentDocumentOption(hostDocument);
                currentDocItem.IsSelected = true; // Default selection
                result.Add(currentDocItem);

                // Get all Revit link instances
                var linkCollector = new FilteredElementCollector(hostDocument)
                    .OfClass(typeof(RevitLinkInstance));

                foreach (RevitLinkInstance linkInstance in linkCollector)
                {
                    try
                    {
                        // Get the linked document
                        var linkedDocument = GetLinkedDocument(linkInstance);
                        if (linkedDocument != null)
                        {
                            var linkedItem = LinkedDocumentItem.CreateLinkedDocumentOption(linkInstance, linkedDocument);
                            result.Add(linkedItem);

                            _logger.Debug($"Found linked document: {linkedDocument.Title}");
                        }
                        else
                        {
                            _logger.Warn($"Link instance {linkInstance.Name} has no loaded document");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Error processing link instance {linkInstance.Name}: {ex.Message}");
                    }
                }

                _logger.Info($"Found {result.Count - 1} linked documents (plus Current Document option)");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting linked documents: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Gets the linked Document from a RevitLinkInstance
        /// </summary>
        /// <param name="linkInstance">The Revit link instance</param>
        /// <returns>The linked document, or null if not available</returns>
        public Document GetLinkedDocument(RevitLinkInstance linkInstance)
        {
            if (linkInstance == null)
                return null;

            try
            {
                return linkInstance.GetLinkDocument();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting linked document from link instance: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the transform from the host document to the linked document
        /// </summary>
        /// <param name="linkInstance">The Revit link instance</param>
        /// <returns>The transform representing the link's position in the host model</returns>
        public Transform GetLinkTransform(RevitLinkInstance linkInstance)
        {
            if (linkInstance == null)
                return Transform.Identity;

            try
            {
                return linkInstance.GetTransform();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting link transform: {ex.Message}");
                return Transform.Identity;
            }
        }

        /// <summary>
        /// Gets the inverse transform for converting points from host to linked document space
        /// </summary>
        /// <param name="linkInstance">The Revit link instance</param>
        /// <returns>The inverse transform</returns>
        public Transform GetInverseLinkTransform(RevitLinkInstance linkInstance)
        {
            var transform = GetLinkTransform(linkInstance);
            try
            {
                return transform.Inverse;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error getting inverse link transform: {ex.Message}");
                return Transform.Identity;
            }
        }

        /// <summary>
        /// Transforms a point from host document coordinates to linked document coordinates
        /// </summary>
        /// <param name="point">The point in host document coordinates</param>
        /// <param name="transform">The inverse link transform</param>
        /// <returns>The point in linked document coordinates</returns>
        public XYZ TransformPointToLinkedDocument(XYZ point, Transform transform)
        {
            if (point == null)
                return null;

            try
            {
                return transform.OfPoint(point);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error transforming point to linked document: {ex.Message}");
                return point;
            }
        }

        /// <summary>
        /// Transforms a bounding box from host document coordinates to linked document coordinates
        /// </summary>
        /// <param name="bbox">The bounding box in host document coordinates</param>
        /// <param name="transform">The inverse link transform</param>
        /// <returns>The bounding box in linked document coordinates</returns>
        public BoundingBoxXYZ TransformBoundingBoxToLinkedDocument(BoundingBoxXYZ bbox, Transform transform)
        {
            if (bbox == null)
                return null;

            try
            {
                var transformedBbox = new BoundingBoxXYZ();
                transformedBbox.Min = transform.OfPoint(bbox.Min);
                transformedBbox.Max = transform.OfPoint(bbox.Max);
                transformedBbox.Transform = Transform.Identity;
                return transformedBbox;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error transforming bounding box to linked document: {ex.Message}");
                return bbox;
            }
        }

        /// <summary>
        /// Checks if a linked document item represents the current document
        /// </summary>
        /// <param name="linkedDocItem">The linked document item to check</param>
        /// <returns>True if this is the current document option</returns>
        public bool IsCurrentDocument(LinkedDocumentItem linkedDocItem)
        {
            return linkedDocItem?.IsCurrentDocument == true;
        }

        /// <summary>
        /// Gets the appropriate document for spatial queries based on the selected linked document
        /// </summary>
        /// <param name="hostDocument">The active/host document</param>
        /// <param name="selectedLinkedDoc">The selected linked document item (may be Current Document)</param>
        /// <returns>The document to use for spatial queries (host or linked)</returns>
        public Document GetSourceDocument(Document hostDocument, LinkedDocumentItem selectedLinkedDoc)
        {
            if (selectedLinkedDoc == null || selectedLinkedDoc.IsCurrentDocument)
            {
                return hostDocument;
            }

            return selectedLinkedDoc.LinkedDocument;
        }

        /// <summary>
        /// Gets the appropriate transform for coordinate conversion based on the selected linked document
        /// Returns identity transform for Current Document option
        /// </summary>
        /// <param name="selectedLinkedDoc">The selected linked document item</param>
        /// <returns>The transform to use for coordinate conversion</returns>
        public Transform GetTransformForCoordinateConversion(LinkedDocumentItem selectedLinkedDoc)
        {
            if (selectedLinkedDoc == null || selectedLinkedDoc.IsCurrentDocument)
            {
                return Transform.Identity;
            }

            return selectedLinkedDoc.GetInverseTransform();
        }

        /// <summary>
        /// Transforms an element's point to the linked document space if needed
        /// </summary>
        /// <param name="point">The point in host document coordinates</param>
        /// <param name="selectedLinkedDoc">The selected linked document item</param>
        /// <returns>The point in the appropriate coordinate space</returns>
        public XYZ TransformPointIfNeeded(XYZ point, LinkedDocumentItem selectedLinkedDoc)
        {
            if (point == null)
                return null;

            if (selectedLinkedDoc == null || selectedLinkedDoc.IsCurrentDocument)
            {
                return point;
            }

            var transform = selectedLinkedDoc.GetInverseTransform();
            return TransformPointToLinkedDocument(point, transform);
        }

        /// <summary>
        /// Transforms an element's bounding box to the linked document space if needed
        /// </summary>
        /// <param name="bbox">The bounding box in host document coordinates</param>
        /// <param name="selectedLinkedDoc">The selected linked document item</param>
        /// <returns>The bounding box in the appropriate coordinate space</returns>
        public BoundingBoxXYZ TransformBoundingBoxIfNeeded(BoundingBoxXYZ bbox, LinkedDocumentItem selectedLinkedDoc)
        {
            if (bbox == null)
                return null;

            if (selectedLinkedDoc == null || selectedLinkedDoc.IsCurrentDocument)
            {
                return bbox;
            }

            var transform = selectedLinkedDoc.GetInverseTransform();
            return TransformBoundingBoxToLinkedDocument(bbox, transform);
        }
    }
}
