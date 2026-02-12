using System;

namespace Aps.Core.Models;

/// <summary>
/// Represents an APS Parameters API group.
/// Groups are the first level of organization under an account.
/// Currently, only one group is supported per account with matching group ID to the account ID.
/// </summary>
public class ApsGroup
{
    /// <summary>
    /// Unique identifier for the group
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the group
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Account ID that this group belongs to
    /// </summary>
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Description of the group
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Override ToString for better display in ComboBox
    /// </summary>
    public override string ToString() => string.IsNullOrEmpty(Name) ? Id : Name;
}
