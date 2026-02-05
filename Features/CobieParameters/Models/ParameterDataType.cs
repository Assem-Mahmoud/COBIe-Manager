namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// Parameter data types supported by Revit
/// </summary>
public enum ParameterDataType
{
    /// <summary>
    /// Text string
    /// </summary>
    Text,

    /// <summary>
    /// Integer number
    /// </summary>
    Integer,

    /// <summary>
    /// Number (decimal)
    /// </summary>
    Number,

    /// <summary>
    /// Length measurement
    /// </summary>
    Length,

    /// <summary>
    /// Area measurement
    /// </summary>
    Area,

    /// <summary>
    /// Volume measurement
    /// </summary>
    Volume,

    /// <summary>
    /// Angle measurement
    /// </summary>
    Angle,

    /// <summary>
    /// Family type element reference
    /// </summary>
    FamilyType,

    /// <summary>
    /// Yes/No boolean
    /// </summary>
    YesNo,

    /// <summary>
    /// Multi-value selection
    /// </summary>
    MultiValue,

    /// <summary>
    /// Unknown or unsupported type
    /// </summary>
    Unknown
}
