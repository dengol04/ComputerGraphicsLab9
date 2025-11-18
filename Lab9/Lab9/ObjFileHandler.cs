using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using Lab9;

namespace Lab9
{
    public static class ObjFileHandler
    {
        public static Polyhedron LoadFromObj(string filePath)
        {
            var vertices = new List<Vector3>();
            var vertexNormals = new List<Vector3>();
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
                                vertices.Add(new Vector3(x, y, z));
                            }
                        }
                        else if (parts[0] == "vn" && parts.Length >= 4)
                        {
                            if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double x) &&
                                double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double y) &&
                                double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double z))
                            {
                                vertexNormals.Add(new Vector3(x, y, z).Normalize());
                            }
                        }
                        else if (parts[0] == "f" && parts.Length >= 4)
                        {
                            var indices = new List<int>();
                            for (int i = 1; i < parts.Length; i++)
                            {
                                string part = parts[i];
                                string indexPart = part.Split('/')[0];
                                if (int.TryParse(indexPart, out int index))
                                {
                                    indices.Add(index - 1);
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
                throw new Exception("Ошибка при чтении OBJ файла: " + ex.Message);
            }

            if (!vertices.Any() || !polygons.Any())
            {
                throw new Exception("Файл OBJ не содержит данных о вершинах или гранях.");
            }

            double minX = vertices.Min(v => v.X);
            double minY = vertices.Min(v => v.Y);
            double minZ = vertices.Min(v => v.Z);
            double maxX = vertices.Max(v => v.X);
            double maxY = vertices.Max(v => v.Y);
            double maxZ = vertices.Max(v => v.Z);

            double centerX = (minX + maxX) / 2.0;
            double centerY = (minY + maxY) / 2.0;
            double centerZ = (minZ + maxZ) / 2.0;

            double maxDimension = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
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

            List<Vector3> normalsToPass = null;
            if (vertexNormals.Count > 0 && vertexNormals.Count == normalizedVertices.Count)
            {
                normalsToPass = vertexNormals;
            }

            return new Polyhedron(normalizedVertices, polygons, normalsToPass);
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