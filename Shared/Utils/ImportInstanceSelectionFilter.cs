using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace COBIeManager.Shared.Utils
{
    public class ImportInstanceSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem) => elem is ImportInstance;
        public bool AllowReference(Reference reference, XYZ position) => true;
    }
}
