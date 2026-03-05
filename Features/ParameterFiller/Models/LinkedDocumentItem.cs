using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace COBIeManager.Features.ParameterFiller.Models
{
    /// <summary>
    /// Represents a linked Revit document that can be selected as a data source
    /// for parameter filling operations. Includes a special "Current Document" option
    /// for backward compatibility with existing workflows.
    /// </summary>
    public partial class LinkedDocumentItem : ObservableObject
    {
        /// <summary>
        /// Creates the special "Current Document" option
        /// </summary>
        public static LinkedDocumentItem CreateCurrentDocumentOption(Document currentDocument)
        {
            return new LinkedDocumentItem(currentDocument, null, "Current Document");
        }

        /// <summary>
        /// Creates a linked document option
        /// </summary>
        public static LinkedDocumentItem CreateLinkedDocumentOption(RevitLinkInstance linkInstance, Document linkedDocument)
        {
            if (linkInstance == null)
                throw new ArgumentNullException(nameof(linkInstance));
            if (linkedDocument == null)
                throw new ArgumentNullException(nameof(linkedDocument));

            // Get the link type name for display
            string displayName = linkedDocument.Title ?? "Unnamed Link";
            return new LinkedDocumentItem(linkedDocument, linkInstance, displayName);
        }

        /// <summary>
        /// Private constructor
        /// </summary>
        private LinkedDocumentItem(Document linkedDocument, RevitLinkInstance linkInstance, string displayName)
        {
            LinkedDocument = linkedDocument ?? throw new ArgumentNullException(nameof(linkedDocument));
            LinkInstance = linkInstance;
            DisplayName = displayName;
            IsCurrentDocument = (linkInstance == null);
        }

        /// <summary>
        /// The linked Revit document (or current document for the special option)
        /// </summary>
        public Document LinkedDocument { get; }

        /// <summary>
        /// The Revit link instance (null for Current Document option)
        /// </summary>
        public RevitLinkInstance LinkInstance { get; }

        /// <summary>
        /// Display name shown in the UI dropdown
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Indicates whether this is the special "Current Document" option
        /// </summary>
        public bool IsCurrentDocument { get; }

        /// <summary>
        /// Whether this document is currently selected in the UI
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Gets the transform from the host document to this linked document
        /// Returns identity transform for Current Document option
        /// </summary>
        public Transform GetTransform()
        {
            if (IsCurrentDocument || LinkInstance == null)
            {
                return Transform.Identity;
            }

            try
            {
                return LinkInstance.GetTransform();
            }
            catch
            {
                return Transform.Identity;
            }
        }

        /// <summary>
        /// Gets the inverse transform for converting points from host to linked document space
        /// </summary>
        public Transform GetInverseTransform()
        {
            var transform = GetTransform();
            try
            {
                return transform.Inverse;
            }
            catch
            {
                return Transform.Identity;
            }
        }
    }
}
