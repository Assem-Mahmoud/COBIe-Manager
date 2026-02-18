using System;

namespace COBIeManager.Shared.Models
{
    /// <summary>
    /// Defines the fill operation mode for parameter filling (flags for multi-select)
    /// </summary>
    [Flags]
    public enum FillMode
    {
        /// <summary>
        /// No fill mode selected
        /// </summary>
        None = 0,

        /// <summary>
        /// Fill level parameters
        /// </summary>
        Level = 1,        // 1 << 0

        /// <summary>
        /// Fill room name parameters
        /// </summary>
        RoomName = 2,    // 1 << 1

        /// <summary>
        /// Fill room number parameters
        /// </summary>
        RoomNumber = 4,   // 1 << 2

        /// <summary>
        /// Fill box ID from Model Groups
        /// </summary>
        Groups = 8        // 1 << 3
    }
}
