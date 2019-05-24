using System;

namespace NUlid.Rng
{
    public class MonotonicUlidRng : BaseUlidRng
    {
        private const int DEFAULTMSBMASKBITS = 10;

        // Internal RNG to base initial values for the current millisecond on
        private readonly IUlidRng _rng;
        private readonly byte[] _mask = new byte[RANDLEN];

        // Object to lock() on while generating Id's
        private readonly object _genlock = new object();

        // When an id was generated the last time (UNIX time in ms)
        private long _lastgen;
        private byte[] _lastvalue;

        public MonotonicUlidRng()
            : this(DEFAULTMSBMASKBITS) { }

        public MonotonicUlidRng(int maskMsbBits)
            : this(DEFAULTRNG, maskMsbBits) { }

        public MonotonicUlidRng(IUlidRng rng)
            : this(rng, DEFAULTMSBMASKBITS) { }

        public MonotonicUlidRng(IUlidRng rng, int maskMsbBits = DEFAULTMSBMASKBITS, byte[] lastvalue = null, DateTimeOffset? intializeLastGen = null)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            var maskbits = maskMsbBits >= 0 && maskMsbBits <= 80 - DEFAULTMSBMASKBITS ? maskMsbBits : throw new ArgumentOutOfRangeException(nameof(maskMsbBits));

            _lastvalue = lastvalue ?? new byte[RANDLEN];
            if (_lastvalue.Length != RANDLEN)
                throw new InvalidOperationException($"{nameof(lastvalue)} must be {RANDLEN} bytes long");

            _lastgen = Ulid.ToUnixTimeMilliseconds(intializeLastGen ?? Ulid.EPOCH);

            //Prepare mask
            for (var i = 0; i < _mask.Length; i++)
            {
                var bits = maskbits > 8 ? 8 : maskbits; // Calculate number of bits to mask from this byte
                maskbits -= bits;                       // Decrease number of bits needing to mask total
                _mask[i] = (byte)((1 << 8 - bits) - 1);
            }
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
                    for (var i = 0; i < _mask.Length && _mask[i] < 255; i++)
                        _lastvalue[i] = (byte)(_lastvalue[i] & _mask[i]);

                    _lastgen = timestamp;
                }
                return _lastvalue;
            }
        }
    }
}
