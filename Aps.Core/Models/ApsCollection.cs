using System;

namespace Aps.Core.Models;

/// <summary>
/// Represents an APS Parameters API collection.
/// Collections are the second level of organization within a group.
/// </summary>
public class ApsCollection
{
    /// <summary>
    /// Unique identifier for the collection
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the collection
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Group ID that this collection belongs to
    /// </summary>
    public string GroupId { get; set; } = string.Empty;

    /// <summary>
    /// Account ID that this collection belongs to
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Description of the collection
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is the default COBie collection
    /// </summary>
    public bool IsDefaultCobieCollection { get; set; }

    /// <summary>
    /// Override ToString for better display in ComboBox
    /// </summary>
    public override string ToString() => string.IsNullOrEmpty(Name) ? Id : Name;
}
