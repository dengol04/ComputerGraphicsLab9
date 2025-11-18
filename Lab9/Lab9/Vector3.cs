using System;

namespace Lab9
{
    public class Vector3
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vector3 operator *(Vector3 v, double scalar)
        {
            return new Vector3(v.X * scalar, v.Y * scalar, v.Z * scalar);
        }

        public static Vector3 operator *(double scalar, Vector3 v)
        {
            return v * scalar;
        }

        public static Vector3 operator *(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public double Dot(Vector3 other)
        {
            return X * other.X + Y * other.Y + Z * other.Z;
        }

        public Vector3 Cross(Vector3 other)
        {
            double newX = Y * other.Z - Z * other.Y;
            double newY = Z * other.X - X * other.Z;
            double newZ = X * other.Y - Y * other.X;
            return new Vector3(newX, newY, newZ);
        }

        public Vector3 Normalize()
        {
            double length = Length;
            if (length == 0) return new Vector3(0, 0, 0);
            return new Vector3(X / length, Y / length, Z / length);
        }
    }
}