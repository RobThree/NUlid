using System;

namespace NUlid.Rng
{
    /// <summary>
    /// An RNG that increments the random part by 1 bit each time a random value is requested within the same
    /// millisecond.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This 'wrapper' RNG generates random bytes based on a specified RNG (which can be any RNG that
    ///         implements <see cref="IUlidRng"/>. However, when <see cref="GetRandomBytes(DateTimeOffset)"/> is called
    ///         within the same millisecond, the last generated value + 1 will be returned instead. This causes
    ///         "monotonic increasing" values.
    ///     </para>
    ///     <para>
    ///         To ensure there are enough values *within* the same millisecond the generated initial random value for
    ///         a given millisecond will have a specified of (most significant) bits set to 0 to help prevent an
    ///         overflow. The default number of (most significant) bits that are set to 0 is 10, but this value can be
    ///         specified and be anything between 0 and 70 where 0 has the highest risk of an initial random value
    ///         being generated close to an overflow and 70 the lowest.
    ///     </para>
    /// </remarks>
    public class MonotonicUlidRng : BaseUlidRng
    {
        // Internal RNG to base initial values for the current millisecond on
        private readonly IUlidRng _rng;

        // Number of MSB's that are masked out by default when generating a new sequence 'seed'
        private const int DEFAULTMSBMASKBITS = 10;
        // 'Pre-computed' mask with which new sequence 'seeds' are masked
        private readonly byte[] _mask = new byte[RANDLEN];

        // Object to lock() on while generating
        private readonly object _genlock = new object();

        // Contains the timestamp of when the GetRandomBytes method was last called
        private long _lastgen;
        // Contains the last generated value
        private byte[] _lastvalue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonotonicUlidRng"/> class with a default
        /// <see cref="IUlidRng"/> and default number of mask bits.
        /// </summary>
        public MonotonicUlidRng()
            : this(DEFAULTMSBMASKBITS) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonotonicUlidRng"/> class with a default
        /// <see cref="IUlidRng"/> and specified number of mask bits.
        /// </summary>
        /// <param name="maskMsbBits">
        /// The number of (most significant) bits to mask out / set to 0 when generating a random value for a given
        /// timestamp
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maskMsbBits"/> is less than 0 or more than 70.
        /// </exception>
        public MonotonicUlidRng(int maskMsbBits)
            : this(DEFAULTRNG, maskMsbBits) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonotonicUlidRng"/> class with a specified
        /// <see cref="IUlidRng"/> and default number of mask bits.
        /// </summary>
        /// <param name="rng">The <see cref="IUlidRng"/> to get the random numbers from.</param>
        public MonotonicUlidRng(IUlidRng rng)
            : this(rng, DEFAULTMSBMASKBITS) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonotonicUlidRng"/> class.
        /// </summary>
        /// <param name="rng">The <see cref="IUlidRng"/> to get the random numbers from.</param>
        /// <param name="maskMsbBits">
        /// The number of (most significant) bits to mask out / set to 0 when generating a random value for a given
        /// timestamp
        /// </param>
        /// <param name="lastValue">The last value to 'continue from'; use <see langword="null"/> for defaults.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rng"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="maskMsbBits"/> is less than 0 or more than 70.
        /// </exception>
        public MonotonicUlidRng(IUlidRng rng, int maskMsbBits = DEFAULTMSBMASKBITS, Ulid? lastValue = null)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            var maskbits = maskMsbBits >= 0 && maskMsbBits <= 80 - DEFAULTMSBMASKBITS ? maskMsbBits : throw new ArgumentOutOfRangeException(nameof(maskMsbBits));

            _lastvalue = lastValue == null ? new byte[RANDLEN] : lastValue.Value.Random;
            _lastgen = Ulid.ToUnixTimeMilliseconds(lastValue == null ? Ulid.EPOCH : lastValue.Value.Time);

            // Prepare (or 'pre-compute') mask
            for (var i = 0; i < _mask.Length; i++)
            {
                var bits = maskbits > 8 ? 8 : maskbits; // Calculate number of bits to mask from this byte
                maskbits -= bits;                       // Decrease number of bits needing to mask total
                _mask[i] = (byte)((1 << 8 - bits) - 1);
            }
        }

        /// <summary>
        /// Creates and returns random bytes based on internal <see cref="IUlidRng"/>.
        /// </summary>
        /// <param name="dateTime">
        /// DateTime for which the random bytes need to be generated; this value is used to determine wether a sequence
        /// needs to be incremented (same timestamp with millisecond resolution) or reset to a new random value.
        /// </param>
        /// <returns>Random bytes.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the specified <paramref name="dateTime"/> is before the last time this method was called.
        /// </exception>
        public override byte[] GetRandomBytes(DateTimeOffset dateTime)
        {
            lock (_genlock)
            {
                // Get unix time for given datetime
                var timestamp = Ulid.ToUnixTimeMilliseconds(dateTime);

                if (timestamp < _lastgen)
                    throw new InvalidOperationException("Clock moved backwards; this is not supported.");

                if (timestamp == _lastgen)  // Same timestamp as last time we generated random values?
                {
                    // Increment our random value by one.
                    var i = RANDLEN;
                    while (--i >= 0 && ++_lastvalue[i] == 0) ;
                    // If i made it all the way to -1 we have an overflow and we throw
                    if (i < 0)
                        throw new OverflowException();
                }
                else // New(er) timestamp, so generate a new random value and store the new(er) timestamp
                {
                    _lastvalue = _rng.GetRandomBytes(dateTime);                 // Use internal RNG to get bytes from
                    for (var i = 0; i < _mask.Length && _mask[i] < 255; i++)    // Mask out desired number of MSB's
                        _lastvalue[i] = (byte)(_lastvalue[i] & _mask[i]);

                    _lastgen = timestamp;   // Store last timestamp
                }
                return _lastvalue;
            }
        }
    }
}
