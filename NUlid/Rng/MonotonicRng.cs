using System;

namespace NUlid.Rng
{
    public class MonotonicRng : BaseRng
    {
        private const int DEFAULTMSBMASKBITS = 10;

        // Internal RNG to base initial values for the current millisecond on
        private readonly IUlidRng _rng;
        private readonly int _maskmsbbits;

        // Object to lock() on while generating Id's
        private readonly object _genlock = new object();

        // When an id was generated the last time (UNIX time in ms)
        private long _lastgen;
        private byte[] _lastvalue;

        public MonotonicRng()
            : this(DEFAULTMSBMASKBITS) { }

        public MonotonicRng(int maskMsbBits)
            : this(DEFAULTRNG, maskMsbBits) { }

        public MonotonicRng(IUlidRng rng)
            : this(rng, DEFAULTMSBMASKBITS) { }

        public MonotonicRng(IUlidRng rng, int maskMsbBits = DEFAULTMSBMASKBITS, byte[] lastvalue = null, DateTimeOffset? intializeLastGen = null)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            _maskmsbbits = maskMsbBits >= 0 && maskMsbBits <= 80 - DEFAULTMSBMASKBITS ? maskMsbBits : throw new ArgumentOutOfRangeException(nameof(maskMsbBits));

            _lastvalue = lastvalue ?? new byte[RANDLEN];
            if (_lastvalue.Length != RANDLEN)
                throw new InvalidOperationException($"{nameof(lastvalue)} must be {RANDLEN} bytes long");

            _lastgen = Ulid.ToUnixTimeMilliseconds(intializeLastGen ?? Ulid.EPOCH);
        }

        public override byte[] GetRandomBytes(DateTimeOffset dateTime)
        {
            lock (_genlock)
            {
                var timestamp = Ulid.ToUnixTimeMilliseconds(dateTime);

                if (timestamp == _lastgen)
                {
                    // Increase last value by one
                    var i = RANDLEN;
                    while (--i >= 0 && ++_lastvalue[i] == 0) ;
                    if (i < 0)
                        throw new OverflowException();
                }
                else // If we're in a new(er) "timeslot", so we can reset the sequence and store the new(er) "timeslot"
                {
                    _lastvalue = _rng.GetRandomBytes(dateTime);
                    // Mask out desired number of MSB's
                    var b = _maskmsbbits;
                    var i = 0;
                    while (b > 0)
                    {
                        var bits = b > 8 ? 8 : b;   // Calculate number of bits to mask from this byte
                        b -= bits;                  // Decrease number of bits needing to mask total
                        _lastvalue[i] = (byte)(_lastvalue[i] & ((1 << 8 - bits) - 1));
                        i++;
                    }

                    _lastgen = timestamp;
                }
                return _lastvalue;
            }
        }
    }
}
