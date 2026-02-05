using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using System.Windows.Controls;
using Orientation = System.Windows.Controls.Orientation;
using System.Windows;

namespace COBIeManager.Shared.Services
{
    

    public static class SnackbarService
    {
        public static SnackbarMessageQueue GlobalMessageQueue { get; set; }
        public static Snackbar GlobalSnackbar { get; set; }

        public static void Initialize(SnackbarMessageQueue queue, Snackbar snackbar)
        {
            GlobalMessageQueue = queue;
            GlobalSnackbar = snackbar;
        }

        private static void ShowMessage(string message, Brush backgroundBrush, PackIconKind iconKind)
        {
            if (GlobalSnackbar != null)
            {
                GlobalSnackbar.Background = backgroundBrush;
            }

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var icon = new PackIcon
            {
                Kind = iconKind,
                Width = 20,
                Height = 20,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 300 // Optional, limit how wide it can go
            };


            panel.Children.Add(icon);
            panel.Children.Add(textBlock);

            GlobalMessageQueue?.Enqueue(panel);
        }

        public static void ShowSuccess(string message)
        {
            ShowMessage(message, Brushes.Green, PackIconKind.CheckCircleOutline);
        }

        public static void ShowError(string message)
        {
            ShowMessage(message, Brushes.Red, PackIconKind.AlertCircleOutline);
        }

        public static void ShowInfo(string message)
        {
            ShowMessage(message, Brushes.DeepSkyBlue, PackIconKind.InformationOutline);
        }

        public static void ShowWarning(string message)
        {
            ShowMessage(message, Brushes.Orange, PackIconKind.AlertOutline);
        }
    }

}
