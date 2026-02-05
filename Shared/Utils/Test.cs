using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COBIeManager.Shared.Utils
{
    public class Test
    {
        public static void VisualizeGeometry(Document doc,IList<GeometryObject> objects)
        {
            DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel)).SetShape(objects);

        }

        /// <summary>
        /// Visualizes door centroids and insertion points as small boxes for debugging coordinate placement.
        /// Creates visible small cubes at each point location.
        /// </summary>
        public static void VisualizeDoorPoints(Document doc, List<XYZ> centroids, List<XYZ> insertionPoints, double boxSize = 1.0)
        {
            if (centroids == null || centroids.Count == 0)
                return;

            try
            {
                // Create boxes for centroids
                var centroidGeometry = new List<GeometryObject>();
                foreach (var centroid in centroids)
                {
                    // Create a small box (cube) centered at the point
                    XYZ[] verts = new XYZ[]
                    {
                        new XYZ(centroid.X - boxSize/2, centroid.Y - boxSize/2, centroid.Z - boxSize/2),
                        new XYZ(centroid.X + boxSize/2, centroid.Y - boxSize/2, centroid.Z - boxSize/2),
                        new XYZ(centroid.X + boxSize/2, centroid.Y + boxSize/2, centroid.Z - boxSize/2),
                        new XYZ(centroid.X - boxSize/2, centroid.Y + boxSize/2, centroid.Z - boxSize/2),
                        new XYZ(centroid.X - boxSize/2, centroid.Y - boxSize/2, centroid.Z + boxSize/2),
                        new XYZ(centroid.X + boxSize/2, centroid.Y - boxSize/2, centroid.Z + boxSize/2),
                        new XYZ(centroid.X + boxSize/2, centroid.Y + boxSize/2, centroid.Z + boxSize/2),
                        new XYZ(centroid.X - boxSize/2, centroid.Y + boxSize/2, centroid.Z + boxSize/2)
                    };

                    // Create edges
                    int[] indices = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };

                    for (int i = 0; i < indices.Length; i += 2)
                    {
                        try
                        {
                            Line edge = Line.CreateBound(verts[indices[i]], verts[indices[i + 1]]);
                            centroidGeometry.Add(edge);
                        }
                        catch { }
                    }
                }

                if (centroidGeometry.Count > 0)
                {
                    try
                    {
                        var shape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                        shape.SetShape(centroidGeometry);
                        shape.Name = $"DEBUG_Centroids ({centroids.Count})";
                    }
                    catch { }
                }

                // Create boxes for insertion points (for comparison)
                if (insertionPoints != null && insertionPoints.Count > 0)
                {
                    var insertionGeometry = new List<GeometryObject>();
                    foreach (var point in insertionPoints)
                    {
                        double halfBox = (boxSize * 0.6) / 2;
                        XYZ[] verts2 = new XYZ[]
                        {
                            new XYZ(point.X - halfBox, point.Y - halfBox, point.Z - halfBox),
                            new XYZ(point.X + halfBox, point.Y - halfBox, point.Z - halfBox),
                            new XYZ(point.X + halfBox, point.Y + halfBox, point.Z - halfBox),
                            new XYZ(point.X - halfBox, point.Y + halfBox, point.Z - halfBox),
                            new XYZ(point.X - halfBox, point.Y - halfBox, point.Z + halfBox),
                            new XYZ(point.X + halfBox, point.Y - halfBox, point.Z + halfBox),
                            new XYZ(point.X + halfBox, point.Y + halfBox, point.Z + halfBox),
                            new XYZ(point.X - halfBox, point.Y + halfBox, point.Z + halfBox)
                        };

                        int[] indices2 = { 0, 1, 1, 2, 2, 3, 3, 0, 4, 5, 5, 6, 6, 7, 7, 4, 0, 4, 1, 5, 2, 6, 3, 7 };

                        for (int i = 0; i < indices2.Length; i += 2)
                        {
                            try
                            {
                                Line edge = Line.CreateBound(verts2[indices2[i]], verts2[indices2[i + 1]]);
                                insertionGeometry.Add(edge);
                            }
                            catch { }
                        }
                    }

                    if (insertionGeometry.Count > 0)
                    {
                        try
                        {
                            var shape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                            shape.SetShape(insertionGeometry);
                            shape.Name = $"DEBUG_InsertionPoints ({insertionPoints.Count})";
                        }
                        catch { }
                    }
                }
            }
            catch (Exception)
            {
                // Silently fail - visualization is debug-only
            }
        }

    }
}
