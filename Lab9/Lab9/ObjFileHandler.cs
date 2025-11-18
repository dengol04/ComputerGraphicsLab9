using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

namespace Lab9
{
    public static class ObjFileHandler
    {
        public static Polyhedron LoadFromObj(string filePath)
        {
            var vertices = new List<Vector3>();
            var polygons = new List<Polygon>();

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    {
                        continue;
                    }

                    string[] parts = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length > 0)
                    {
                        if (parts[0] == "v" && parts.Length >= 4)
                        {
                            if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
                                double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double y) &&
                                double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double z))
                            {
                                vertices.Add(new Vector3(x, -y, z));
                            }
                        }
                        else if (parts[0] == "f" && parts.Length >= 4)
                        {
                            var indices = new List<int>();
                            for (int i = 1; i < parts.Length; i++)
                            {
                                string indexPart = parts[i].Split('/')[0];

                                if (int.TryParse(indexPart, out int oneBasedIndex))
                                {
                                    if (oneBasedIndex > 0)
                                    {
                                        indices.Add(oneBasedIndex - 1);
                                    }
                                }
                            }

                            if (indices.Count >= 3)
                            {
                                polygons.Add(new Polygon(indices.ToArray()));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при загрузке OBJ файла: " + ex.Message);
            }

            if (vertices.Count == 0)
            {
                throw new Exception("OBJ файл не содержит вершин.");
            }

            double minX = vertices.Min(v => v.X);
            double maxX = vertices.Max(v => v.X);
            double minY = vertices.Min(v => v.Y);
            double maxY = vertices.Max(v => v.Y);
            double minZ = vertices.Min(v => v.Z);
            double maxZ = vertices.Max(v => v.Z);

            double centerX = (minX + maxX) / 2.0;
            double centerY = (minY + maxY) / 2.0;
            double centerZ = (minZ + maxZ) / 2.0;

            double scaleX = maxX - minX;
            double scaleY = maxY - minY;
            double scaleZ = maxZ - minZ;

            double maxDimension = Math.Max(scaleX, Math.Max(scaleY, scaleZ));

            double targetSize = 2.0;
            double scaleFactor = targetSize / maxDimension;

            var normalizedVertices = new List<Vector3>();
            foreach (var v in vertices)
            {
                double newX = v.X - centerX;
                double newY = v.Y - centerY;
                double newZ = v.Z - centerZ;

                newX *= scaleFactor;
                newY *= scaleFactor;
                newZ *= scaleFactor;

                normalizedVertices.Add(new Vector3(newX, newY, newZ));
            }

            return new Polyhedron(normalizedVertices, polygons);
        }

        public static void SaveToObj(Polyhedron polyhedron, string filePath)
        {
            var lines = new List<string>();

            foreach (var v in polyhedron.Vertices)
            {
                lines.Add("v " + v.X.ToString(CultureInfo.InvariantCulture) + " " + v.Y.ToString(CultureInfo.InvariantCulture) + " " + v.Z.ToString(CultureInfo.InvariantCulture));
            }

            foreach (var p in polyhedron.Polygons)
            {
                string faceLine = "f " + string.Join(" ", p.Indices.Select(i => (i + 1).ToString()));
                lines.Add(faceLine);
            }

            try
            {
                File.WriteAllLines(filePath, lines);
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при сохранении OBJ файла: " + ex.Message);
            }
        }
    }
}