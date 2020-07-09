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

        // Object to lock() on while generating
        private readonly object _genlock = new object();

        // Contains the timestamp of when the GetRandomBytes method was last called
        private long _lastgen;
        // Contains the last generated value
        private byte[] _lastvalue;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonotonicUlidRng"/> class with a default
        /// <see cref="IUlidRng"/>.
        /// </summary>
        public MonotonicUlidRng()
            : this(DEFAULTRNG) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MonotonicUlidRng"/> class.
        /// </summary>
        /// <param name="rng">The <see cref="IUlidRng"/> to get the random numbers from.</param>
        /// <param name="lastValue">The last value to 'continue from'; use <see langword="null"/> for defaults.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="rng"/> is null.</exception>
        public MonotonicUlidRng(IUlidRng rng, Ulid? lastValue = null)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));

            _lastvalue = lastValue == null ? new byte[RANDLEN] : lastValue.Value.Random;
            _lastgen = Ulid.ToUnixTimeMilliseconds(lastValue == null ? Ulid.EPOCH : lastValue.Value.Time);
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

                if (timestamp <= _lastgen)  // Same or earlier timestamp as last time we generated random values?
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
                    _lastvalue[0] = (byte)(_lastvalue[0] & 0x7F);               // Mask out bit 0 of the random part

                    _lastgen = timestamp;   // Store last timestamp
                }
                return _lastvalue;
            }
        }
    }
}
