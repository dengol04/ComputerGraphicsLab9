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
            var vertexUVs = new List<Vector2UV>();
            var polygons = new List<Polygon>();

            try
            {
                string[] lines = File.ReadAllLines(filePath);

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                        continue;

                    string[] parts = trimmedLine.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;

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
                    else if (parts[0] == "vt" && parts.Length >= 3)
                    {
                        if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double u) &&
                            double.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
                        {
                            vertexUVs.Add(new Vector2UV(u, v));
                        }
                    }
                    else if (parts[0] == "f" && parts.Length >= 4)
                    {
                        var indices = new List<int>();
                        var uvs = new List<Vector2UV>();

                        for (int i = 1; i < parts.Length; i++)
                        {
                            string part = parts[i];
                            string[] subParts = part.Split('/');

                            if (int.TryParse(subParts[0], out int vertexIndex))
                            {
                                indices.Add(vertexIndex - 1);
                            }

                            if (subParts.Length > 1 && int.TryParse(subParts[1], out int uvIndex))
                            {
                                uvs.Add(vertexUVs[uvIndex - 1]);
                            }
                        }

                        if (indices.Count >= 3)
                        {
                            polygons.Add(new Polygon(indices.ToArray(), uvs.Count == indices.Count ? uvs.ToArray() : null));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка при чтении OBJ файла: " + ex.Message);
            }

            if (!vertices.Any() || !polygons.Any())
                throw new Exception("Файл OBJ не содержит данных о вершинах или гранях.");

            double minX = vertices.Min(v => v.X), minY = vertices.Min(v => v.Y), minZ = vertices.Min(v => v.Z);
            double maxX = vertices.Max(v => v.X), maxY = vertices.Max(v => v.Y), maxZ = vertices.Max(v => v.Z);
            double centerX = (minX + maxX) / 2.0, centerY = (minY + maxY) / 2.0, centerZ = (minZ + maxZ) / 2.0;
            double maxDimension = Math.Max(maxX - minX, Math.Max(maxY - minY, maxZ - minZ));
            double scaleFactor = 2.0 / maxDimension;

            var normalizedVertices = vertices.Select(v => new Vector3(
                (v.X - centerX) * scaleFactor,
                (v.Y - centerY) * scaleFactor,
                (v.Z - centerZ) * scaleFactor)).ToList();

            List<Vector3> normalsToPass = vertexNormals.Count == normalizedVertices.Count ? vertexNormals : null;

            return new Polyhedron(normalizedVertices, polygons, normalsToPass);
        }

        public static void SaveToObj(Polyhedron polyhedron, string filePath)
        {
            var lines = new List<string>();

            foreach (var v in polyhedron.Vertices)
            {
                lines.Add($"v {v.X.ToString(CultureInfo.InvariantCulture)} {v.Y.ToString(CultureInfo.InvariantCulture)} {v.Z.ToString(CultureInfo.InvariantCulture)}");
            }

            int uvCounter = 1;
            foreach (var p in polyhedron.Polygons)
            {
                if (p.UV != null)
                {
                    foreach (var uv in p.UV)
                    {
                        lines.Add($"vt {uv.U.ToString(CultureInfo.InvariantCulture)} {uv.V.ToString(CultureInfo.InvariantCulture)}");
                    }
                }
            }

            foreach (var p in polyhedron.Polygons)
            {
                if (p.UV != null && p.UV.Length == p.Indices.Length)
                {
                    string faceLine = "f " + string.Join(" ", p.Indices.Select((vi, idx) => $"{vi + 1}/{uvCounter++}"));
                    lines.Add(faceLine);
                }
                else
                {
                    string faceLine = "f " + string.Join(" ", p.Indices.Select(i => (i + 1).ToString()));
                    lines.Add(faceLine);
                }
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
