using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace NUlid.Tests
{
    [TestClass]
    public class UlidTests
    {

        //TODO: Test min/max timestamps (7ZZZZZZZZZ == ??)
        //                              (0000000000 == UNIX EPOCH)
        //                               76EZ91ZPZZ == Actual DateTimeOffset.MaxValue
        //                              and other edge-cases maybe
        //  and vice versa/overflows/whatever

        [TestMethod]
        public void NewUlid_Creates_NewUlid()
        {
            var target = Ulid.NewUlid();

            Assert.AreEqual(26, target.ToString().Length);
        }

        [TestMethod]
        public void NewUlid_Uses_SpecifiedRNG()
        {
            var target = Ulid.NewUlid(new FakeUlidRng());
            CollectionAssert.AreEqual(FakeUlidRng.DEFAULTRESULT, target.Random);
        }


        [TestMethod]
        public void NewUlid_Uses_SpecifiedTime()
        {
            // We can't "simply" use DateTimeOffset.UtcNow since the resolution of a ulid is milliseconds, and UtcNow
            // has microseconds and more. So we drop that part by converting to UnixTimeMilliseconds and then back to
            // a DateTimeOffset.
            var time = DateTimeOffset.FromUnixTimeMilliseconds(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var target = Ulid.NewUlid(time);
            Assert.AreEqual(time, target.Time);
        }

        [TestMethod]
        public void Guid_CanConvertTo_Ulid()
        {
            var g = Guid.NewGuid();
            var u = new Ulid(g);

            Assert.AreEqual(g, u.ToGuid());
        }

        [TestMethod]
        public void Ulid_ToString_EncodesCorrectly()
        {
            var target = Ulid.NewUlid(DateTimeOffset.FromUnixTimeMilliseconds(1469918176385), new FakeUlidRng());

            Assert.AreEqual(26, target.ToString().Length);
            Assert.IsTrue(target.ToString().StartsWith("01ARYZ6S41"));
            Assert.IsTrue(target.ToString().EndsWith("DEADBEEFDEADBEEF"));
        }

        [TestMethod]
        public void Ulid_Empty_IsCorrectValue()
        {
            var target = Ulid.Empty;

            Assert.AreEqual(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc), target.Time);
            Assert.IsTrue(target.Random.All(v => v == 0));
            Assert.AreEqual(new string('0', 26), target.ToString());
        }

        [TestMethod]
        public void Ulid_Parse_ParsesCorrectly()
        {
            var ulid = Ulid.NewUlid();
            var target = Ulid.Parse(ulid.ToString());

            Assert.IsTrue(target.Random.SequenceEqual(ulid.Random));
            Assert.AreEqual(ulid.Time, target.Time);
        }
    }

}
