using System;

namespace Aps.Core.Models;

/// <summary>
/// Represents an Autodesk Construction Cloud project.
/// </summary>
public class ApsProject
{
    /// <summary>
    /// Unique identifier for the project
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the project
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ID of the root folder for this project
    /// </summary>
    public string RootFolderId { get; set; } = string.Empty;

    /// <summary>
    /// Hub ID that this project belongs to
    /// </summary>
    public string HubId { get; set; } = string.Empty;

    /// <summary>
    /// Override ToString for better display in ComboBox
    /// </summary>
    public override string ToString() => string.IsNullOrEmpty(Name) ? Id : Name;
}
