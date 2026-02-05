using COBIeManager.Shared.ViewModels;
using System;

namespace COBIeManager.Shared.Services
{
    /// <summary>
    /// Static service for controlling the shared loading overlay across all features.
    /// Provides a centralized way to show/hide loading states with progress tracking.
    /// </summary>
    public static class LoadingOverlayService
    {
        private static LoadingOverlayViewModel _viewModel;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets the shared ViewModel instance for the loading overlay.
        /// This should be bound to the LoadingOverlay control's DataContext.
        /// </summary>
        public static LoadingOverlayViewModel ViewModel
        {
            get
            {
                if (_viewModel == null)
                {
                    lock (_lock)
                    {
                        if (_viewModel == null)
                        {
                            _viewModel = new LoadingOverlayViewModel();
                        }
                    }
                }
                return _viewModel;
            }
        }

        /// <summary>
        /// Shows the loading overlay with a simple message and indeterminate spinner.
        /// </summary>
        /// <param name="message">Main message to display (e.g., "Creating rooms...")</param>
        /// <param name="subMessage">Optional sub-message for additional details</param>
        public static void Show(string message, string subMessage = "")
        {
            ViewModel.Show(message, subMessage);
        }

        /// <summary>
        /// Shows the loading overlay with a progress bar.
        /// </summary>
        /// <param name="message">Main message to display</param>
        /// <param name="currentValue">Current progress value</param>
        /// <param name="maximum">Maximum progress value (default 100)</param>
        /// <param name="subMessage">Optional sub-message</param>
        public static void ShowWithProgress(string message, double currentValue = 0, double maximum = 100, string subMessage = "")
        {
            ViewModel.ShowWithProgress(message, currentValue, maximum, subMessage);
        }

        /// <summary>
        /// Updates the progress value and optional sub-message.
        /// </summary>
        /// <param name="value">New progress value</param>
        /// <param name="subMessage">Optional sub-message to update</param>
        public static void UpdateProgress(double value, string subMessage = null)
        {
            ViewModel.UpdateProgress(value, subMessage);
        }

        /// <summary>
        /// Updates only the message text without changing visibility or progress.
        /// </summary>
        /// <param name="message">New main message</param>
        /// <param name="subMessage">Optional new sub-message</param>
        public static void UpdateMessage(string message, string subMessage = null)
        {
            ViewModel.UpdateMessage(message, subMessage);
        }

        /// <summary>
        /// Hides the loading overlay.
        /// </summary>
        public static void Hide()
        {
            ViewModel.Hide();
        }

        /// <summary>
        /// Gets whether the overlay is currently visible.
        /// </summary>
        public static bool IsVisible => ViewModel.IsVisible;
    }
}
