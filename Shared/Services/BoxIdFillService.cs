using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;
using COBIeManager.Shared.Interfaces;
using COBIeManager.Shared.Logging;
using COBIeManager.Shared.Models;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Service for filling ACG-BOX-ID parameters from Model GroupType names.
    /// Processes ALL instances of each group type.
    /// Supports linked documents as group data source.
    /// </summary>
    public class BoxIdFillService : IBoxIdFillService
    {
        private readonly ILogger _logger;
        private readonly ILinkedDocumentService _linkService;

        public BoxIdFillService(ILogger logger, ILinkedDocumentService linkService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _linkService = linkService ?? throw new ArgumentNullException(nameof(linkService));
        }

        /// <summary>
        /// Gets the source document and transform for group queries.
        /// Returns the linked document if selected, otherwise the host document.
        /// </summary>
        private (Document sourceDoc, Transform transform) GetSourceDocumentAndTransform(Document hostDocument, FillConfiguration config)
        {
            if (config?.General?.SelectedLinkedDocument == null ||
                config.General.SelectedLinkedDocument.IsCurrentDocument)
            {
                return (hostDocument, null);
            }

            var linkedDoc = config.General.SelectedLinkedDocument.LinkedDocument;
            var transform = config.General.SelectedLinkedDocument.GetInverseTransform();
            return (linkedDoc, transform);
        }

        /// <summary>
        /// Analyzes groups and returns preview summary without modifying document.
        /// Counts all instances across all group types.
        /// </summary>
        public BoxIdFillPreviewSummary PreviewFill(Document document, FillConfiguration config)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            _logger.Info("Starting box ID fill preview (GroupType-based)");

            // Get source document (may be linked)
            var (sourceDoc, transform) = GetSourceDocumentAndTransform(document, config);
            if (!sourceDoc.Equals(document))
            {
                _logger.Info($"Using linked document for groups: {sourceDoc.Title}");
            }

            var summary = new BoxIdFillPreviewSummary();

            // Collect all model groups from source document
            var groups = CollectModelGroups(sourceDoc);

            if (!groups.Any())
            {
                _logger.Info("No model groups found in document");
                return summary;
            }

            _logger.Info($"Found {groups.Count} model group instances");

            // Group by GroupType.Id to count unique group types
            var groupsByType = groups
                .Where(g => g.GroupType != null)
                .GroupBy(g => g.GroupType.Id)
                .ToList();

            _logger.Info($"Found {groupsByType.Count} unique group types");

            int totalMembers = 0;
            int skippedNoName = 0;
            int typesWithNoName = 0;

            foreach (var typeGroup in groupsByType)
            {
                var representativeGroup = typeGroup.First();
                var groupType = representativeGroup.GroupType;
                string typeName = groupType.Name?.Trim();

                if (string.IsNullOrEmpty(typeName))
                {
                    typesWithNoName++;
                    skippedNoName += typeGroup.Count();
                    _logger.Debug($"GroupType {groupType.Id} has no name, skipping {typeGroup.Count()} instances");
                    continue;
                }

                // Count members from ALL instances of this type
                var memberIds = representativeGroup.GetMemberIds();
                int membersPerInstance = memberIds.Count;
                int totalMembersForType = membersPerInstance * typeGroup.Count();
                totalMembers += totalMembersForType;

                _logger.Debug($"GroupType '{typeName}' ({typeGroup.Count()} instances) has {membersPerInstance} members per instance = {totalMembersForType} total members");
            }

            // Count all instances (not just types) excluding those without names
            int totalInstances = groupsByType.Sum(g => g.Count());
            int namedInstances = totalInstances - skippedNoName;

            summary.EstimatedGroupsToProcess = namedInstances;
            summary.EstimatedMembersToProcess = totalMembers;
            summary.GroupsSkippedNoName = skippedNoName;

            _logger.Info($"Preview complete: {summary.EstimatedGroupsToProcess} group instances, {summary.EstimatedMembersToProcess} total members");

            return summary;
        }

        /// <summary>
        /// Executes box ID fill operation using GroupType.Name as the value.
        /// Processes ALL instances of each group type.
        /// </summary>
        public BoxIdFillSummary ExecuteFill(
            Document document,
            FillConfiguration config,
            Action<int, string> progressAction = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            // Get parameter name from config
            var selectedParams = config.GetGroupModeParameters();
            if (selectedParams.Count == 0)
            {
                _logger.Warn("No parameters mapped to Group mode");
                return new BoxIdFillSummary();
            }

            string parameterName = selectedParams[0];
            bool overwriteExisting = config.OverwriteExisting;

            _logger.Info($"Starting box ID fill operation (parameter: {parameterName}, overwrite: {overwriteExisting})");

            // Get source document (may be linked)
            var (sourceDoc, transform) = GetSourceDocumentAndTransform(document, config);
            if (!sourceDoc.Equals(document))
            {
                _logger.Info($"Using linked document for groups: {sourceDoc.Title}");
            }

            var stopwatch = Stopwatch.StartNew();
            var summary = new BoxIdFillSummary();

            // Collect all model groups from source document
            var groups = CollectModelGroups(sourceDoc);

            if (!groups.Any())
            {
                _logger.Info("No model groups found in document");
                summary.ProcessingDuration = stopwatch.Elapsed;
                return summary;
            }

            // Group by GroupType.Id - we'll process only ONE instance per type
            var groupsByType = groups
                .Where(g => g.GroupType != null)
                .GroupBy(g => g.GroupType.Id)
                .ToList();

            summary.TotalGroupsScanned = groups.Count;
            _logger.Info($"Processing {groups.Count} model group instances ({groupsByType.Count} unique types)");

            // Determine if we're using a linked document
            bool usingLinkedDocument = !sourceDoc.Equals(document);

            // Create failure preprocessor for group-related warnings
            var failurePreprocessor = new GroupFailurePreprocessor(_logger);

            // Get selected categories for filtering
            var selectedCategories = config.GetSelectedCategories().ToList();

            // Pre-collect elements from host document if using linked document (for spatial matching)
            IList<Element> hostElements = null;
            if (usingLinkedDocument && selectedCategories.Any())
            {
                hostElements = CollectElementsByCategories(document, selectedCategories);
                _logger.Info($"Collected {hostElements.Count} elements from host document for spatial matching");
            }

            // Use transaction with failure preprocessor
            using (var transaction = new Transaction(document, "Fill ACG-BOX-ID from GroupTypes"))
            {
                // Configure failure handling
                var failureOptions = transaction.GetFailureHandlingOptions();
                failureOptions.SetFailuresPreprocessor(failurePreprocessor);
                transaction.SetFailureHandlingOptions(failureOptions);

                transaction.Start();

                try
                {
                    int processedTypes = 0;
                    int processedInstances = 0;
                    int totalMembersProcessed = 0;
                    int totalInstances = groupsByType.Sum(g => g.Count());

                    foreach (var typeGroup in groupsByType)
                    {
                        processedTypes++;

                        // Get the group type from the first instance
                        var firstInstance = typeGroup.First();
                        var groupType = firstInstance.GroupType;

                        // Use GroupType.Name as the value (not Group.Name)
                        string typeName = groupType.Name?.Trim();

                        if (string.IsNullOrEmpty(typeName))
                        {
                            summary.GroupsSkippedNoName += typeGroup.Count();
                            _logger.Debug($"GroupType {groupType.Id} has no name, skipping {typeGroup.Count()} instances");
                            continue;
                        }

                        _logger.Debug($"Processing GroupType '{typeName}' (type {processedTypes}/{groupsByType.Count}) - " +
                                    $"Type has {typeGroup.Count()} instances");

                        // Report progress
                        if (processedInstances % 5 == 0 || processedInstances + typeGroup.Count() >= totalInstances)
                        {
                            var message = $"Processing group instances: {processedInstances}/{totalInstances} - '{typeName}'";
                            _logger.Debug(message);
                            progressAction?.Invoke(processedInstances, message);
                        }

                        // Process ALL instances of this GroupType
                        foreach (var groupInstance in typeGroup)
                        {
                            processedInstances++;

                            if (usingLinkedDocument)
                            {
                                // For linked documents, use spatial matching
                                var groupBbox = groupInstance.get_BoundingBox(null);
                                if (groupBbox != null)
                                {
                                    // Transform bounding box to host document space
                                    BoundingBoxXYZ hostBbox = TransformBoundingBoxToHostSpace(groupBbox, transform);

                                    // Find elements within this group's bounding box
                                    var elementsInGroup = FindElementsInBoundingBox(hostElements, hostBbox);

                                    _logger.Debug($"Group '{groupInstance.Name}' has spatial match with {elementsInGroup.Count} elements in host document");

                                    foreach (var element in elementsInGroup)
                                    {
                                        summary.TotalMembersScanned++;

                                        // Skip nested groups
                                        if (element is Group)
                                        {
                                            summary.MembersSkippedNestedGroup++;
                                            summary.RegisterSkippedElement(element.Id.IntegerValue, SkipReasons.NestedGroup);
                                            continue;
                                        }

                                        // Try to set the parameter using GroupType.Name as value
                                        var result = TrySetParameter(element, parameterName, typeName, overwriteExisting);

                                        if (result.Success)
                                        {
                                            summary.MembersUpdated++;
                                        }
                                        else if (result.Skipped)
                                        {
                                            summary.RegisterSkippedElement(element.Id.IntegerValue, result.SkipReason);

                                            switch (result.SkipReason)
                                            {
                                                case SkipReasons.ParameterMissing:
                                                    summary.MembersSkippedParameterMissing++;
                                                    break;
                                                case SkipReasons.ParameterReadOnly:
                                                    summary.MembersSkippedParameterReadOnly++;
                                                    break;
                                                case SkipReasons.ValueExists:
                                                    summary.MembersSkippedValueExists++;
                                                    break;
                                                case SkipReasons.NestedGroup:
                                                    summary.MembersSkippedNestedGroup++;
                                                    break;
                                            }
                                        }
                                    }

                                    totalMembersProcessed += elementsInGroup.Count;
                                }
                            }
                            else
                            {
                                // For current document, use direct member access
                                var memberIds = groupInstance.GetMemberIds();
                                totalMembersProcessed += memberIds.Count;

                                // Update each member of this specific group instance
                                foreach (var memberId in memberIds)
                                {
                                    summary.TotalMembersScanned++;

                                    var member = document.GetElement(memberId);
                                    if (member == null)
                                    {
                                        _logger.Warn($"Member {memberId} not found in document");
                                        continue;
                                    }

                                    // Skip nested groups
                                    if (member is Group)
                                    {
                                        summary.MembersSkippedNestedGroup++;
                                        summary.RegisterSkippedElement(member.Id.IntegerValue, SkipReasons.NestedGroup);
                                        _logger.Debug($"Skipping nested group {member.Id}");
                                        continue;
                                    }

                                    // Try to set the parameter using GroupType.Name as value
                                    var result = TrySetParameter(member, parameterName, typeName, overwriteExisting);

                                    if (result.Success)
                                    {
                                        summary.MembersUpdated++;
                                    }
                                    else if (result.Skipped)
                                    {
                                        summary.RegisterSkippedElement(member.Id.IntegerValue, result.SkipReason);

                                        switch (result.SkipReason)
                                        {
                                            case SkipReasons.ParameterMissing:
                                                summary.MembersSkippedParameterMissing++;
                                                break;
                                            case SkipReasons.ParameterReadOnly:
                                                summary.MembersSkippedParameterReadOnly++;
                                                break;
                                            case SkipReasons.ValueExists:
                                                summary.MembersSkippedValueExists++;
                                                break;
                                            case SkipReasons.NestedGroup:
                                                summary.MembersSkippedNestedGroup++;
                                                break;
                                        }
                                    }
                                }
                            }
                        }

                        // Count each instance processed, not just the type
                        summary.GroupsProcessed += typeGroup.Count();
                    }

                    transaction.Commit();

                    // Add statistics from failure preprocessor
                    summary.FailuresResolved = failurePreprocessor.ResolvedCount;

                    summary.ProcessingDuration = stopwatch.Elapsed;

                    _logger.Info($"Box ID fill complete in {summary.ProcessingDuration.TotalSeconds:F2} seconds");
                    _logger.Info($"Processed {summary.GroupsProcessed} group instances, {summary.MembersUpdated} members updated");
                    if (failurePreprocessor.ResolvedCount > 0)
                    {
                        _logger.Info($"Resolved {failurePreprocessor.ResolvedCount} group-related failure(s)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error during box ID fill operation", ex);
                    transaction.RollBack();
                    throw;
                }
            }

            return summary;
        }

        /// <summary>
        /// Collects elements from the host document by selected categories for spatial matching
        /// </summary>
        private IList<Element> CollectElementsByCategories(Document document, IList<BuiltInCategory> categories)
        {
            var elements = new List<Element>();

            try
            {
                foreach (var category in categories)
                {
                    var collector = new FilteredElementCollector(document)
                        .OfCategory(category)
                        .WhereElementIsNotElementType()
                        .WhereElementIsViewIndependent();

                    elements.AddRange(collector.ToElements());
                }

                _logger.Debug($"Collected {elements.Count} elements from {categories.Count} categories");
            }
            catch (Exception ex)
            {
                _logger.Error("Error collecting elements by category", ex);
            }

            return elements;
        }

        /// <summary>
        /// Transforms a bounding box from linked document space to host document space
        /// </summary>
        private BoundingBoxXYZ TransformBoundingBoxToHostSpace(BoundingBoxXYZ linkedBbox, Transform inverseTransform)
        {
            if (linkedBbox == null)
                return null;

            if (inverseTransform == null)
                return linkedBbox;

            try
            {
                // The inverseTransform converts from host to linked, so we need the inverse of that
                // to convert from linked to host
                var hostTransform = inverseTransform.Inverse;

                var hostBbox = new BoundingBoxXYZ
                {
                    Min = hostTransform.OfPoint(linkedBbox.Min),
                    Max = hostTransform.OfPoint(linkedBbox.Max),
                    Transform = Transform.Identity
                };

                // Ensure Min is actually the minimum and Max is the maximum
                // (after transformation, the points might be swapped)
                if (hostBbox.Min.X > hostBbox.Max.X)
                {
                    var temp = hostBbox.Min.X;
                    hostBbox.Min = new XYZ(hostBbox.Max.X, hostBbox.Min.Y, hostBbox.Min.Z);
                    hostBbox.Max = new XYZ(temp, hostBbox.Max.Y, hostBbox.Max.Z);
                }
                if (hostBbox.Min.Y > hostBbox.Max.Y)
                {
                    var temp = hostBbox.Min.Y;
                    hostBbox.Min = new XYZ(hostBbox.Min.X, hostBbox.Max.Y, hostBbox.Min.Z);
                    hostBbox.Max = new XYZ(hostBbox.Max.X, temp, hostBbox.Max.Z);
                }
                if (hostBbox.Min.Z > hostBbox.Max.Z)
                {
                    var temp = hostBbox.Min.Z;
                    hostBbox.Min = new XYZ(hostBbox.Min.X, hostBbox.Min.Y, hostBbox.Max.Z);
                    hostBbox.Max = new XYZ(hostBbox.Max.X, hostBbox.Max.Y, temp);
                }

                return hostBbox;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error transforming bounding box: {ex.Message}");
                return linkedBbox; // Return original if transformation fails
            }
        }

        /// <summary>
        /// Finds elements that are within or intersect a bounding box
        /// </summary>
        private IList<Element> FindElementsInBoundingBox(IList<Element> elements, BoundingBoxXYZ bbox)
        {
            var result = new List<Element>();

            if (elements == null || bbox == null)
                return result;

            double tolerance = 0.0001; // Small tolerance for floating point comparison

            foreach (var element in elements)
            {
                try
                {
                    var elementBbox = element.get_BoundingBox(null);
                    if (elementBbox == null)
                        continue;

                    // Check if element bounding box intersects with the group bounding box
                    if (BoundingBoxesIntersect(elementBbox, bbox, tolerance))
                    {
                        result.Add(element);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Error checking element {element.Id}: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if two bounding boxes intersect
        /// </summary>
        private bool BoundingBoxesIntersect(BoundingBoxXYZ box1, BoundingBoxXYZ box2, double tolerance)
        {
            // Check for intersection in all three dimensions
            bool xIntersect = box1.Max.X >= box2.Min.X - tolerance && box1.Min.X <= box2.Max.X + tolerance;
            bool yIntersect = box1.Max.Y >= box2.Min.Y - tolerance && box1.Min.Y <= box2.Max.Y + tolerance;
            bool zIntersect = box1.Max.Z >= box2.Min.Z - tolerance && box1.Min.Z <= box2.Max.Z + tolerance;

            return xIntersect && yIntersect && zIntersect;
        }

        /// <summary>
        /// Collects all Model Group instances from the document
        /// </summary>
        private IList<Group> CollectModelGroups(Document document)
        {
            var groups = new List<Group>();

            try
            {
                // Use FilteredElementCollector for model groups
                var collector = new FilteredElementCollector(document)
                    .OfCategory(BuiltInCategory.OST_IOSModelGroups)
                    .WhereElementIsNotElementType();

                foreach (var element in collector)
                {
                    if (element is Group group)
                    {
                        groups.Add(group);
                    }
                }

                _logger.Debug($"Collected {groups.Count} model groups");
            }
            catch (Exception ex)
            {
                _logger.Error("Error collecting model groups", ex);
            }

            return groups;
        }

        /// <summary>
        /// Attempts to set a parameter value on an element
        /// </summary>
        private ParameterSetResult TrySetParameter(Element element, string parameterName, string value, bool overwriteExisting)
        {
            // Skip nested groups
            if (element is Group)
            {
                return new ParameterSetResult
                {
                    Success = false,
                    Skipped = true,
                    SkipReason = SkipReasons.NestedGroup
                };
            }

            // Lookup parameter by name
            var parameter = element.LookupParameter(parameterName);

            if (parameter == null)
            {
                return new ParameterSetResult
                {
                    Success = false,
                    Skipped = true,
                    SkipReason = SkipReasons.ParameterMissing
                };
            }

            // Check if parameter is read-only
            if (parameter.IsReadOnly)
            {
                return new ParameterSetResult
                {
                    Success = false,
                    Skipped = true,
                    SkipReason = SkipReasons.ParameterReadOnly
                };
            }

            // Check if parameter has a value and we're not overwriting
            if (!overwriteExisting)
            {
                var currentValue = parameter.AsString();
                if (!string.IsNullOrEmpty(currentValue))
                {
                    return new ParameterSetResult
                    {
                        Success = false,
                        Skipped = true,
                        SkipReason = SkipReasons.ValueExists
                    };
                }
            }

            // Set the parameter value
            try
            {
                parameter.Set(value);
                return new ParameterSetResult
                {
                    Success = true,
                    Skipped = false
                };
            }
            catch (Exception ex)
            {
                _logger.Warn($"Failed to set parameter '{parameterName}' on element {element.Id}: {ex.Message}");
                return new ParameterSetResult
                {
                    Success = false,
                    Skipped = false,
                    SkipReason = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Result of attempting to set a parameter
        /// </summary>
        private class ParameterSetResult
        {
            public bool Success { get; set; }
            public bool Skipped { get; set; }
            public string SkipReason { get; set; }
        }
    }
}
