using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace COBIeManager.Shared.ViewModels
{
    /// <summary>
    /// ViewModel for the shared loading overlay control.
    /// Manages visibility, message, and progress state.
    /// </summary>
    public partial class LoadingOverlayViewModel : ObservableObject
    {
        /// <summary>
        /// Controls whether the overlay is visible.
        /// </summary>
        [ObservableProperty]
        private bool isVisible;

        /// <summary>
        /// Main message displayed in the overlay (e.g., "Creating rooms...").
        /// </summary>
        [ObservableProperty]
        private string message = "Loading...";

        /// <summary>
        /// Optional sub-message for additional details (e.g., "Created 5 of 20 rooms").
        /// </summary>
        [ObservableProperty]
        private string subMessage = string.Empty;

        /// <summary>
        /// Whether to show a determinate progress bar (true) or indeterminate spinner (false).
        /// </summary>
        [ObservableProperty]
        private bool showProgress;

        /// <summary>
        /// Progress percentage (0-100) for determinate progress.
        /// </summary>
        [ObservableProperty]
        private double progressValue;

        /// <summary>
        /// Maximum value for progress (default 100).
        /// </summary>
        [ObservableProperty]
        private double progressMaximum = 100;

        /// <summary>
        /// Shows the overlay with a message.
        /// </summary>
        public void Show(string message, string subMessage = "")
        {
            Message = message;
            SubMessage = subMessage;
            ShowProgress = false;
            IsVisible = true;
        }

        /// <summary>
        /// Shows the overlay with a progress bar.
        /// </summary>
        public void ShowWithProgress(string message, double currentValue, double maximum, string subMessage = "")
        {
            Message = message;
            SubMessage = subMessage;
            ShowProgress = true;
            ProgressValue = currentValue;
            ProgressMaximum = maximum;
            IsVisible = true;
        }

        /// <summary>
        /// Updates the progress value and optional sub-message.
        /// </summary>
        public void UpdateProgress(double value, string subMessage = null)
        {
            ProgressValue = value;
            if (subMessage != null)
            {
                SubMessage = subMessage;
            }
        }

        /// <summary>
        /// Updates only the message text without changing visibility.
        /// </summary>
        public void UpdateMessage(string message, string subMessage = null)
        {
            Message = message;
            if (subMessage != null)
            {
                SubMessage = subMessage;
            }
        }

        /// <summary>
        /// Hides the overlay.
        /// </summary>
        public void Hide()
        {
            IsVisible = false;
        }
    }
}
