using Autodesk.Revit.DB;
using COBIeManager.Shared.Logging;
using System;

namespace COBIeManager.Shared.Adapters
{
    /// <summary>
    /// Factory for creating version-specific Revit adapters.
    /// </summary>
    public class RevitVersionAdapterFactory
    {
        private readonly ILogger _logger;

        public RevitVersionAdapterFactory(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates an adapter for the specified Revit version.
        /// </summary>
        public IRevitVersionAdapter CreateAdapter(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            string versionString = document.Application.VersionNumber;
            _logger.Info($"Creating version adapter for Revit {versionString}");

            // Extract major version from version string (e.g., "2024.2" -> 2024)
            int majorVersion = int.Parse(versionString.Split('.')[0]);

            return majorVersion switch
            {
                2023 => new RevitVersionAdapter2023(),
                2024 => new RevitVersionAdapter2024(),
                _ => throw new NotSupportedException($"Revit version {majorVersion} is not supported. Supported versions: 2023, 2024")
            };
        }
    }
}
