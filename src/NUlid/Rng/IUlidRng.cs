namespace NUlid.Rng
{
    /// <summary>
    /// Defines the interface for ulid Random Number Generators.
    /// </summary>
    public interface IUlidRng
    {
        /// <summary>
        /// Creates and returns the specified number of random bytes.
        /// </summary>
        /// <param name="length">The desired number of random bytes.</param>
        /// <returns>The specified number of random bytes.</returns>
        byte[] GetRandomBytes(int length);
    }
}
