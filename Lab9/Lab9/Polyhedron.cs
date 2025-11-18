using System;
using System.Collections.Generic;

namespace Lab9
{
    public class Polyhedron
    {
        public List<Vector3> Vertices { get; }
        public List<Polygon> Polygons { get; }

        public Polyhedron(List<Vector3> vertices, List<Polygon> polygons)
        {
            Vertices = vertices;
            Polygons = polygons;
        }

        public static Polyhedron CreateTetrahedron()
        {
            var vertices = new List<Vector3>
            {
                new Vector3(1, 1, 1),
                new Vector3(-1, -1, 1),
                new Vector3(-1, 1, -1),
                new Vector3(1, -1, -1)
            };

            var polygons = new List<Polygon>
            {
                new Polygon(new[] {0, 2, 1}),
                new Polygon(new[] {0, 3, 2}),
                new Polygon(new[] {0, 1, 3}),
                new Polygon(new[] {1, 2, 3})
            };

            return new Polyhedron(vertices, polygons);
        }

        public static Polyhedron CreateHexahedron()
        {
            var vertices = new List<Vector3>
            {
                new Vector3(-1, -1, -1),
                new Vector3(1, -1, -1),
                new Vector3(1, 1, -1),
                new Vector3(-1, 1, -1),
                new Vector3(-1, -1, 1),
                new Vector3(1, -1, 1),
                new Vector3(1, 1, 1),
                new Vector3(-1, 1, 1)
            };

            var polygons = new List<Polygon>
            {
                new Polygon(new[] {0, 1, 2, 3}),
                new Polygon(new[] {4, 7, 6, 5}),
                new Polygon(new[] {0, 4, 5, 1}),
                new Polygon(new[] {2, 6, 7, 3}),
                new Polygon(new[] {0, 3, 7, 4}),
                new Polygon(new[] {1, 5, 6, 2})
            };

            return new Polyhedron(vertices, polygons);
        }

        public static Polyhedron CreateOctahedron()
        {
            var vertices = new List<Vector3>
            {
                new Vector3(1, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(0, 1, 0),
                new Vector3(0, -1, 0),
                new Vector3(0, 0, 1),
                new Vector3(0, 0, -1)
            };

            var polygons = new List<Polygon>
            {
                new Polygon(new[] {4, 0, 2}),
                new Polygon(new[] {4, 2, 1}),
                new Polygon(new[] {4, 1, 3}),
                new Polygon(new[] {4, 3, 0}),
                new Polygon(new[] {5, 2, 0}),
                new Polygon(new[] {5, 0, 3}),
                new Polygon(new[] {5, 3, 1}),
                new Polygon(new[] {5, 1, 2})
            };

            return new Polyhedron(vertices, polygons);
        }

        public static Polyhedron CreateIcosahedron()
        {
            double t = (1.0 + Math.Sqrt(5.0)) / 2.0;

            var vertices = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };

            var polygons = new List<Polygon>
            {
                new Polygon(new[] {0, 11, 5}), new Polygon(new[] {0, 5, 1}), new Polygon(new[] {0, 1, 7}), new Polygon(new[] {0, 7, 10}), new Polygon(new[] {0, 10, 11}),
                new Polygon(new[] {1, 5, 9}), new Polygon(new[] {5, 11, 4}), new Polygon(new[] {11, 10, 2}), new Polygon(new[] {10, 7, 6}), new Polygon(new[] {7, 1, 8}),
                new Polygon(new[] {3, 9, 4}), new Polygon(new[] {3, 4, 2}), new Polygon(new[] {3, 2, 6}), new Polygon(new[] {3, 6, 8}), new Polygon(new[] {3, 8, 9}),
                new Polygon(new[] {4, 9, 5}), new Polygon(new[] {2, 4, 11}), new Polygon(new[] {6, 2, 10}), new Polygon(new[] {8, 6, 7}), new Polygon(new[] {9, 8, 1})
            };

            return new Polyhedron(vertices, polygons);
        }

        public static Polyhedron CreateDodecahedron()
        {
            double t = (1.0 + Math.Sqrt(5.0)) / 2.0;
            double a = 1.0 / t;

            var vertices = new List<Vector3>
            {
                new Vector3(1, 1, 1), new Vector3(1, 1, -1), new Vector3(1, -1, 1), new Vector3(1, -1, -1),
                new Vector3(-1, 1, 1), new Vector3(-1, 1, -1), new Vector3(-1, -1, 1), new Vector3(-1, -1, -1),

                new Vector3(0, a, t), new Vector3(0, a, -t), new Vector3(0, -a, t), new Vector3(0, -a, -t),
                new Vector3(a, t, 0), new Vector3(a, -t, 0), new Vector3(-a, t, 0), new Vector3(-a, -t, 0),

                new Vector3(t, 0, a), new Vector3(t, 0, -a), new Vector3(-t, 0, a), new Vector3(-t, 0, -a)
            };

            var polygons = new List<Polygon>
            {
                new Polygon(new[] {0, 12, 1, 17, 16}),
                new Polygon(new[] {0, 16, 2, 10, 8}),
                new Polygon(new[] {0, 8, 4, 14, 12}),

                new Polygon(new[] {1, 9, 5, 14, 12}),
                new Polygon(new[] {1, 17, 3, 11, 9}),

                new Polygon(new[] {2, 13, 3, 17, 16}),
                new Polygon(new[] {2, 10, 6, 15, 13}),

                new Polygon(new[] {3, 11, 7, 15, 13}),

                new Polygon(new[] {4, 18, 6, 10, 8}),
                new Polygon(new[] {4, 14, 5, 19, 18}),

                new Polygon(new[] {5, 9, 11, 7, 19}),
                new Polygon(new[] {6, 18, 19, 7, 15})
            };

            return new Polyhedron(vertices, polygons);
        }
    }
}