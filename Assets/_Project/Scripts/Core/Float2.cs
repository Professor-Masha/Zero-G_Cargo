using System;

namespace ZeroGCargo.Core
{
    /// <summary>Allocation-free, engine-independent two-dimensional vector.</summary>
    public readonly struct Float2
    {
        /// <summary>Horizontal coordinate.</summary>
        public float X { get; }
        /// <summary>Vertical coordinate.</summary>
        public float Y { get; }
        /// <summary>Creates a vector.</summary>
        public Float2(float x, float y) { X = x; Y = y; }
        /// <summary>Squared magnitude.</summary>
        public float SqrMagnitude => X * X + Y * Y;
        /// <summary>Magnitude.</summary>
        public float Magnitude => (float)Math.Sqrt(SqrMagnitude);
        /// <summary>Unit vector, or zero for a zero-length vector.</summary>
        public Float2 Normalized => Magnitude > 0.00001f ? this / Magnitude : default;
        /// <summary>Adds two vectors.</summary>
        public static Float2 operator +(Float2 a, Float2 b) => new Float2(a.X + b.X, a.Y + b.Y);
        /// <summary>Subtracts two vectors.</summary>
        public static Float2 operator -(Float2 a, Float2 b) => new Float2(a.X - b.X, a.Y - b.Y);
        /// <summary>Scales a vector.</summary>
        public static Float2 operator *(Float2 a, float b) => new Float2(a.X * b, a.Y * b);
        /// <summary>Divides a vector by a scalar.</summary>
        public static Float2 operator /(Float2 a, float b) => new Float2(a.X / b, a.Y / b);
        /// <summary>Computes the dot product.</summary>
        public static float Dot(Float2 a, Float2 b) => a.X * b.X + a.Y * b.Y;
        /// <summary>Limits vector magnitude.</summary>
        public static Float2 Clamp(Float2 value, float max) => value.SqrMagnitude > max * max ? value.Normalized * max : value;
        /// <summary>Linearly interpolates between vectors.</summary>
        public static Float2 Lerp(Float2 a, Float2 b, float t) => a + (b - a) * t;
        /// <summary>Checks that both coordinates are finite.</summary>
        public bool IsFinite => !float.IsNaN(X) && !float.IsInfinity(X) && !float.IsNaN(Y) && !float.IsInfinity(Y);
    }
}
