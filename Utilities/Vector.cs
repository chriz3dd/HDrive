using System;
using System.Collections.Generic;

namespace Utilities
{
    public struct Double2
    {
        public double x, y;

        public Double2(double x, double y)
        {
            this.x = x;
            this.y = y;
        }
        public Double2(List<double> c)
        {
            this.x = c[0];
            this.y = c[1];
        }

        public static Double2 operator -(Double2 a, Double2 b)
        {
            Double2 result;
            result.x = a.x - b.x;
            result.y = a.y - b.y;
            return result;
        }

        public void Rotate(double alfa, Double2 rotCenter)
        {
            // subtract center position
            this -= rotCenter;

            // rotation around center
            Double2 coordinates = new Double2
            {
                x = ((this.x) * Math.Cos(alfa) + (this.y) * -Math.Sin(alfa)),
                y = ((this.x) * Math.Sin(alfa) + (this.y) * Math.Cos(alfa))
            };

            this = coordinates;

            // add center position
            this += rotCenter;
        }

        public static Double2 operator +(Double2 a, Double2 b)
        {
            Double2 result;
            result.x = a.x + b.x;
            result.y = a.y + b.y;
            return result;
        }

        public static Double2 operator *(Double2 a, Double2 b)
        {
            Double2 result;
            result.x = a.x * b.x;
            result.y = a.y * b.y;
            return result;
        }
        public static Double2 operator *(Double2 a, double b)
        {
            Double2 multi = new Double2(b, b);
            return multi * a;
        }

        public void multi(double a)
        {
            x *= a;
            y *= a;
        }

        public override string ToString()
        {
            return $"x: {x} y:{y}";
        }
    }

    public struct Vector2
    {
        public Double2 startPoint;
        public double length;
        public double angle;

        public double getLength(Double2 endPoint)
        {
            Double2 delta = startPoint - endPoint;
            return Math.Sqrt(delta.x * delta.x + delta.y * delta.y);
        }

        /// returns angle between start and endpoint in radiants
        public double getAngle(Double2 endPoint)
        {
            Double2 delta = startPoint - endPoint;

            if (Math.Abs(delta.y) < 0.01)
                delta.y = 1;

            return Math.Atan2(delta.x, delta.y);
        }

        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            Vector2 result = new Vector2();
            result.startPoint = a.startPoint - b.startPoint;
            return result;
        }

    }

    public class Vector2D
    {
        public double X, Y;
        public double Length;

        public Vector2D(double x, double y)
        {
            this.X = x;
            this.Y = y;

            Length = Math.Sqrt(x * x + y * y);
        }

        public double ScalarProduct(Vector2D v2)
        {
            return X * v2.X + Y * v2.Y;
        }

        public double getAngle(Vector2D v2)
        {
            return Math.Acos(ScalarProduct(v2) / (Length * v2.Length));
        }

        public void Normalize()
        {
            X /= Length;
            Y /= Length;
        }

        public Vector2D()
        {
            this.X = 0;
            this.Y = 0;
        }

        public double Distance(Vector2D targetValue)
        {
            Vector2D diff = new Vector2D(this.X - targetValue.X, this.Y - targetValue.Y);
            return Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y);
        }
    }

    public struct Double3
    {
        public double X, Y, Z;

        public Double3(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public void Set(double x, double y, double z)
        {
            X = x; Y = y; Z = z;
        }

        public static Double3 operator -(Double3 a, Double3 b)
        {
            return new Double3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public double Length()
        {
            return Math.Sqrt(X * X + Y * Y + Z * Z);
        }
    }

    public class Vector3D
    {
        public double X, Y, Z;
        public double Length;

        public Vector3D(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public Vector3D(Double3 a, Double3 b)
        {
            Double3 diff = a - b;
            Length = Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);

            if (Length != 0)
            {
                X = diff.X / Length;
                Y = diff.Y / Length;
                Z = diff.Z / Length;
            }
        }

        public Vector3D()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }

        public void Normalize()
        {
            this.X /= Length;
            this.Y /= Length;
            this.Z /= Length;
        }

        public double Distance(Vector3D targetValue)
        {
            Vector3D diff = new Vector3D(this.X - targetValue.X, this.Y - targetValue.Y, this.Z - targetValue.Z);
            return Math.Sqrt(diff.X * diff.X + diff.Y * diff.Y + diff.Z * diff.Z);
        }

        public void Set(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void CalcLength()
        {
            Length = Math.Sqrt(X * X + Y * Y + Z * Z);
        }

        //https://stackoverflow.com/questions/14607640/rotating-a-vector-in-3d-space
        public Vector3D RotateX(double phi)
        {
            Vector3D result = new Vector3D();

            result.X = X;
            result.Y = Y * Math.Cos(phi) - Z * Math.Sin(phi);
            result.Z = Y * Math.Sin(phi) + Z * Math.Cos(phi);
            return result;
        }

        public Vector3D RotateY(double phi)
        {
            Vector3D result = new Vector3D();

            result.X = X * Math.Cos(phi) + Z * Math.Sin(phi);
            result.Y = Y;
            result.Z = -X * Math.Sin(phi) + Z * Math.Cos(phi);
            return result;
        }

        public static Vector3D operator -(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3D operator +(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static double operator *(Vector3D a, Vector3D b)
        {
            return (a.X * b.X + a.Y * b.Y + a.Z * b.Z);
        }

        public static Vector3D operator /(Vector3D a, Vector3D b)
        {
            return new Vector3D(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
        }




    }

    public static class SphericalCoordinates
    {
        public static void SphericalToCartesian(double radius, double polar, double elevation, out Vector3D outCart)
        {
            outCart = new Vector3D();
            double a = radius * Math.Cos(elevation);
            outCart.X = a * Math.Cos(polar);
            outCart.Y = radius * Math.Sin(elevation);
            outCart.Z = a * Math.Sin(polar);
        }

        public static void CartesianToSpherical(Vector3D cartCoords, out double outRadius, out double outPolar, out double outElevation)
        {
            outRadius = Math.Sqrt((cartCoords.X * cartCoords.X)
                            + (cartCoords.Y * cartCoords.Y)
                            + (cartCoords.Z * cartCoords.Z));
            outPolar = Math.Atan(cartCoords.Z / cartCoords.X);
            if (cartCoords.X < 0)
                outPolar += Math.PI;
            outElevation = Math.Asin(cartCoords.Y / outRadius);
        }
    }
}
