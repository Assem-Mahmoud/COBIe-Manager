using Autodesk.Revit.DB;

namespace COBIeManager.Shared.Adapters
{
    /// <summary>
    /// Revit 2023 API adapter.
    /// </summary>
    public class RevitVersionAdapter2023 : IRevitVersionAdapter
    {
        public int RevitVersion => 2023;

        public GeometryElement GetImportGeometry(ImportInstance importInstance, bool computeReferences = true)
        {
            var options = new Options { ComputeReferences = computeReferences };
            return importInstance.get_Geometry(options);
        }

        public Parameter GetParameter(Element element, BuiltInParameter builtInParameter)
        {
            return element.get_Parameter(builtInParameter);
        }

        public void SetParameter(Element element, BuiltInParameter builtInParameter, object value)
        {
            var parameter = element.get_Parameter(builtInParameter);
            if (parameter != null && !parameter.IsReadOnly)
            {
                switch (parameter.StorageType)
                {
                    case StorageType.Double:
                        parameter.Set((double)value);
                        break;
                    case StorageType.Integer:
                        parameter.Set((int)value);
                        break;
                    case StorageType.String:
                        parameter.Set((string)value);
                        break;
                    case StorageType.ElementId:
                        parameter.Set((ElementId)value);
                        break;
                }
            }
        }
    }
}
