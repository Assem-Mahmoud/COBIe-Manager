using System;

namespace COBIeManager.Features.CobieParameters.Models;

/// <summary>
/// Represents a group of Revit categories for parameter binding
/// </summary>
public class CategorySet
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Architectural, Structural, or MEP
    /// </summary>
    public Discipline Discipline { get; set; }

    /// <summary>
    /// Array of Revit categories
    /// </summary>
    public RevitCategory[] Categories { get; set; } = Array.Empty<RevitCategory>();

    /// <summary>
    /// Whether this is a default set
    /// </summary>
    public bool IsDefault { get; set; }
}
