using System;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;

namespace COBIeManager.Shared.Utils
{
    internal class ImageUtils
    {
        public static BitmapImage LoadImage(Assembly a, string name)
        {
            BitmapImage image = new BitmapImage();
            try
            {
                var resourceName = a.GetManifestResourceNames().FirstOrDefault(x => x.Contains(name));
                var stream = a.GetManifestResourceStream(resourceName);

                image.BeginInit();
                image.StreamSource = stream;
                image.EndInit();

            }
            catch (Exception)
            {

                // ignore
            }

            return image;
        }
    }
}

