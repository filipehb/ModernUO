using System;
using Server.Random;

namespace Server.Tests;

/// <summary>
/// Replaces <see cref="BuiltInRng.Generator"/> with a <see cref="System.Random"/> that returns
/// fixed or sequenced values, making skill checks and other RNG-dependent code deterministic in tests.
/// Restores the original generator on <see cref="Dispose"/>.
/// <para>Usage:</para>
/// <code>
/// using var rng = new PredictableRandom(10); // Utility.Random(21) returns 10
/// using var seq = new PredictableRandom(15, 5); // successive Next(max) calls
/// </code>
/// </summary>
public sealed class PredictableRandom : IDisposable
{
    private readonly System.Random _original;

    public PredictableRandom(int fixedValue) : this([fixedValue])
    {
    }

    public PredictableRandom(params int[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        if (values.Length == 0)
        {
            throw new ArgumentException("At least one value is required.", nameof(values));
        }

        _original = BuiltInRng.Generator;
        BuiltInRng.Generator = values.Length == 1
            ? new FixedRandom(values[0])
            : new SequenceRandom(values);
    }

    public void Dispose()
    {
        BuiltInRng.Generator = _original;
    }

    private sealed class FixedRandom(int value) : System.Random
    {
        public override int Next() => value;
        public override int Next(int maxValue) => Math.Clamp(value, 0, maxValue - 1);
        public override int Next(int minValue, int maxValue) => Math.Clamp(value + minValue, minValue, maxValue - 1);
        public override long NextInt64() => value;
        public override long NextInt64(long maxValue) => Math.Clamp(value, 0, maxValue - 1);
        public override long NextInt64(long minValue, long maxValue) => Math.Clamp(value + minValue, minValue, maxValue - 1);
        public override double NextDouble() => Math.Clamp(value / 20.0, 0.0, 1.0);

        public override void NextBytes(byte[] buffer) => Array.Fill(buffer, (byte)Math.Clamp(value, 0, 255));
        public override void NextBytes(Span<byte> buffer) => buffer.Fill((byte)Math.Clamp(value, 0, 255));
    }

    private sealed class SequenceRandom(int[] values) : System.Random
    {
        private int _index;

        private int NextValue()
        {
            var value = values[_index];
            _index = (_index + 1) % values.Length;
            return value;
        }

        public override int Next() => NextValue();
        public override int Next(int maxValue) => Math.Clamp(NextValue(), 0, maxValue - 1);
        public override int Next(int minValue, int maxValue) =>
            Math.Clamp(NextValue() + minValue, minValue, maxValue - 1);
        public override long NextInt64() => NextValue();
        public override long NextInt64(long maxValue) => Math.Clamp(NextValue(), 0, maxValue - 1);
        public override long NextInt64(long minValue, long maxValue) =>
            Math.Clamp(NextValue() + minValue, minValue, maxValue - 1);
        public override double NextDouble() => Math.Clamp(NextValue() / 20.0, 0.0, 1.0);

        public override void NextBytes(byte[] buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)Math.Clamp(NextValue(), 0, 255);
            }
        }

        public override void NextBytes(Span<byte> buffer)
        {
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)Math.Clamp(NextValue(), 0, 255);
            }
        }
    }
}
