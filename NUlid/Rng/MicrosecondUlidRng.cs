using System;

namespace NUlid.Rng
{
    /// <summary>
    /// An RNG for the random part of ulid's that uses the number of microseconds of the given date to determine
    /// the final 'random' part of the ULID.
    /// </summary>
    public class MicrosecondUlidRng : IUlidRng
    {
        private readonly IUlidRng _rng;
        private readonly ushort _mask;
        private readonly byte _negmaskmsb;
        private readonly byte _negmasklsb;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosecondUlidRng"/> class.
        /// </summary>
        /// <param name="rng">The <see cref="IUlidRng"/> to get the random numbers from.</param>
        /// <param name="bits">The number of bits to use for the microseconds part.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rng"/> is null.</exception>
        public MicrosecondUlidRng(IUlidRng rng, int bits = 14)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            if (bits < 0 || bits > 14)
            {
                throw new ArgumentOutOfRangeException(nameof(bits));
            }

            // Pre-calculate masks
            _mask = (ushort)(-1 << (15 - bits));  // The number of bits reserved for the microseconds part + 1 to overflow the random part into

            _negmaskmsb = (byte)((~_mask) >> 8);
            _negmasklsb = (byte)((~_mask) & 0xFF);
        }


        /// <summary>
        /// Creates and returns random bytes based on internal <see cref="IUlidRng"/> and the microseconds of the given <paramref name="dateTime"/>..
        /// </summary>
        /// <param name="dateTime">
        /// DateTime for which the random bytes need to be generated; this value is used to determine wether a sequence
        /// needs to be incremented (same timestamp with millisecond resolution) or reset to a new random value.
        /// </param>
        /// <returns>Random bytes.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the specified <paramref name="dateTime"/> is before the last time this method was called.
        /// </exception>
        public byte[] GetRandomBytes(DateTimeOffset dateTime)
        {
            var buffer = _rng.GetRandomBytes(dateTime);
            // Extract microseconds from timestamp (14 bits max), align microseconds-MSB (14 bits) to ushort MSB (16 bits) by shifting left 2
            // bits. Then mask out undesired bits
            var usecpart = (ushort)(((dateTime.Ticks % 10000) << 2) & _mask);

            // We now have the desired number of bits of the microsecond part starting at the MSB; overwrite the first to bytes
            // of random data with the microsecondpart
            buffer[0] = (byte)(buffer[0] & _negmaskmsb | (usecpart >> 8));
            buffer[1] = (byte)(buffer[0] & _negmasklsb | (usecpart & 0xFF));
            return buffer;
        }
    }
}
