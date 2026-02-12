using Autodesk.Revit.DB;
using COBIeManager.Features.ParameterFiller.Models;

namespace COBIeManager.Shared.Interfaces
{
    /// <summary>
    /// Service for filling Revit parameters with level and room information
    /// </summary>
    public interface IParameterFillService
    {
        /// <summary>
        /// Fills parameters for elements with level and room information
        /// </summary>
        /// <param name="elements">Elements to process</param>
        /// <param name="configuration">Fill configuration with parameter mappings</param>
        /// <param name="logger">Logger for tracking results</param>
        /// <returns>Summary of parameter fill operations</returns>
        ParameterAssignmentResult FillParameters(
            IEnumerable<Element> elements,
            FillConfiguration configuration,
            IProcessingLogger logger);

        /// <summary>
        /// Validates that parameters can be safely written to elements
        /// </summary>
        /// <param name="element">Element to validate</param>
        /// <param name="parameterName">Parameter name to write</param>
        /// <param name="overwriteExisting">Whether to overwrite existing values</param>
        /// <returns>True if parameter can be written, false otherwise</param>
        bool CanWriteParameter(
            Element element,
            string parameterName,
            bool overwriteExisting);

        /// <summary>
        /// Writes a parameter value to an element with safety checks
        /// </summary>
        /// <param name="element">Element to write to</param>
        /// <param name="parameterName">Parameter name</param>
        /// <param name="value">Value to write</param>
        /// <param name="overwriteExisting">Whether to overwrite existing values</param>
        /// <returns>True if successful, false otherwise</returns>
        bool WriteParameterSafely(
            Element element,
            string parameterName,
            string value,
            bool overwriteExisting);
    }
}