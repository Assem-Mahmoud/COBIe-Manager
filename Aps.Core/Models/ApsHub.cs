using System;

namespace Aps.Core.Models;

/// <summary>
/// Represents an Autodesk Hub (Account) from APS
/// </summary>
public class ApsHub
{
    /// <summary>
    /// Unique identifier for the hub/account
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the hub
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Hub type (e.g., "a360" for BIM 360, "acc" for Construction Cloud)
    /// </summary>
    public string? HubType { get; set; }

    /// <summary>
    /// Override ToString for better display in ComboBox
    /// </summary>
    public override string ToString() => string.IsNullOrEmpty(Name) ? Id : $"{Name} ({Id})";
}
