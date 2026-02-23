using System;
using System.Collections.Generic;

namespace Aps.Core.Models;

/// <summary>
/// Maps APS parameter group IDs to human-readable group names.
/// APS format examples:
/// - "autodesk.parameter.group:fireProtection-1.0.0" -> "Fire Protection"
/// - "autodesk.parameter.group:text-1.0.0" -> "Text"
/// - "autodesk.revit.paramgroup.PG_DATA-1.0.0" -> "Data"
/// </summary>
public static class ParameterGroupMapper
{
    /// <summary>
    /// Maps APS parameter group IDs to human-readable group names.
    /// </summary>
    public static string GetGroupName(string? apsGroupId)
    {
        if (string.IsNullOrEmpty(apsGroupId))
            return "Data";

        string lowerGroup = apsGroupId.ToLowerInvariant();

        // Handle "autodesk.parameter.group:groupName-1.0.0" format
        if (lowerGroup.Contains("autodesk.parameter.group:"))
        {
            return MapApsParameterGroupName(lowerGroup);
        }

        // Handle "autodesk.revit.paramgroup.PG_XXX-1.0.0" format
        if (lowerGroup.Contains("autodesk.revit.paramgroup.") || lowerGroup.Contains("pg_"))
        {
            return MapRevitParameterGroupName(apsGroupId);
        }

        // Try direct mapping for known group names
        return MapDirectGroupName(lowerGroup);
    }

    /// <summary>
    /// Maps APS parameter group format (autodesk.parameter.group:groupName-1.0.0)
    /// </summary>
    private static string MapApsParameterGroupName(string lowerGroup)
    {
        // Extract the group name between colon and dash
        int colonIndex = lowerGroup.IndexOf(':');
        if (colonIndex >= 0)
        {
            string afterColon = lowerGroup.Substring(colonIndex + 1);
            int dashIndex = afterColon.IndexOf('-');
            string groupName = dashIndex > 0 ? afterColon.Substring(0, dashIndex) : afterColon;

            // Map the extracted group name
            return groupName switch
            {
                "dataplaceholder" => "Data",
                "data" => "Data",
                "identitydata" => "Identity Data",
                "identity" => "Identity Data",
                "constraints" => "Constraints",
                "dimension" => "Dimensions",
                "dimensions" => "Dimensions",
                "geometry" => "Dimensions",
                "structural" => "Structural Analysis",
                "structuralanalysis" => "Structural Analysis",
                "electrical" => "Electrical",
                "electricalcircuiting" => "Electrical",
                "mechanical" => "Mechanical",
                "plumbing" => "Plumbing",
                "fireprotection" => "Fire Protection",
                "fire" => "Fire Protection",
                "energyanalysis" => "Energy Analysis",
                "energy" => "Energy Analysis",
                "lighting" => "Lighting",
                "materials" => "Materials and Finishes",
                "loads" => "Structural Loads",
                "phasing" => "Phasing",
                "reinforcement" => "Reinforcement",
                "rebar" => "Reinforcement",
                "stairs" => "Stairs and Railings",
                "railings" => "Stairs and Railings",
                "text" => "Text",
                "graphics" => "Graphics",
                "general" => "General",
                _ => ToTitleCase(groupName)
            };
        }

        return "Data";
    }

    /// <summary>
    /// Maps Revit parameter group format (autodesk.revit.paramgroup.PG_XXX-1.0.0)
    /// </summary>
    private static string MapRevitParameterGroupName(string apsGroupId)
    {
        Dictionary<string, string> revitGroupMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "PG_DATA", "Data" },
            { "PG_IDENTITY_DATA", "Identity Data" },
            { "PG_CONSTRAINTS", "Constraints" },
            { "PG_GEOMETRY", "Dimensions" },
            { "PG_STRUCTURAL_ANALYSIS", "Structural Analysis" },
            { "PG_ELECTRICAL", "Electrical" },
            { "PG_MECHANICAL", "Mechanical" },
            { "PG_PLUMBING", "Plumbing" },
            { "PG_FIRE_PROTECTION", "Fire Protection" },
            { "PG_ENERGY_ANALYSIS", "Energy Analysis" },
            { "PG_LIGHTING", "Lighting" },
            { "PG_MATERIALS", "Materials and Finishes" },
            { "PHASING", "Phasing" },
            { "PG_REINFORCEMENT", "Reinforcement" },
            { "PG_TEXT", "Text" },
            { "PG_GRAPHICS", "Graphics" },
            { "PG_GENERAL", "General" }
        };

        string cleanId = ExtractGroupName(apsGroupId);
        if (revitGroupMapping.TryGetValue(cleanId, out string? mappedName))
            return mappedName;

        return ToTitleCase(cleanId);
    }

    /// <summary>
    /// Direct mapping for known group names.
    /// </summary>
    private static string MapDirectGroupName(string lowerGroup)
    {
        return lowerGroup switch
        {
            var g when g.Contains("fire") => "Fire Protection",
            var g when g.Contains("text") && !g.Contains("context") => "Text",
            var g when g.Contains("constraint") => "Constraints",
            var g when g.Contains("dimension") => "Dimensions",
            var g when g.Contains("geometry") => "Dimensions",
            var g when g.Contains("identity") => "Identity Data",
            var g when g.Contains("structural") => "Structural Analysis",
            var g when g.Contains("electrical") => "Electrical",
            var g when g.Contains("mechanical") => "Mechanical",
            var g when g.Contains("plumbing") => "Plumbing",
            var g when g.Contains("energy") => "Energy Analysis",
            var g when g.Contains("lighting") => "Lighting",
            var g when g.Contains("material") => "Materials and Finishes",
            var g when g.Contains("load") => "Structural Loads",
            var g when g.Contains("phase") => "Phasing",
            var g when g.Contains("reinforcement") || g.Contains("rebar") => "Reinforcement",
            var g when g.Contains("stair") || g.Contains("railing") => "Stairs and Railings",
            var g when g.Contains("graphic") => "Graphics",
            _ => "Data"
        };
    }

    /// <summary>
    /// Extracts the group name from an APS/Revit group ID.
    /// </summary>
    private static string ExtractGroupName(string groupId)
    {
        if (string.IsNullOrEmpty(groupId))
            return string.Empty;

        // Find the last colon or dot separator
        int lastColon = groupId.LastIndexOf(':');
        int lastDot = groupId.LastIndexOf('.');
        int startIndex = Math.Max(lastColon, lastDot) + 1;

        if (startIndex < groupId.Length)
        {
            string groupPart = groupId.Substring(startIndex);

            // Remove version suffix
            int dashIndex = groupPart.IndexOf('-');
            if (dashIndex > 0)
            {
                groupPart = groupPart.Substring(0, dashIndex);
            }

            return groupPart;
        }

        return groupId;
    }

    /// <summary>
    /// Converts a string to title case (e.g., "fireProtection" -> "Fire Protection").
    /// </summary>
    private static string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "Data";

        // Add spaces before uppercase letters and capitalize first letter
        string formatted = System.Text.RegularExpressions.Regex.Replace(input, "(?<!^)([A-Z])", " $1");
        return char.ToUpper(formatted[0]) + (formatted.Length > 1 ? formatted.Substring(1) : "");
    }
}
