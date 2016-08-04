using NUlid.Rng;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NUlid.Performance
{
    class Program
    {
        static void Main(string[] args)
        {
            int iterations = 1000000;

            Time<object>((p, it) =>
                {
                    for (int i = 0; i < it; i++)
                        Guid.NewGuid();
                }, iterations, 
                "Guid.NewGuid():                                               {0,15:N0}/sec"
            );

            Time<object>(
                (p, it) =>
                {
                    var simplerng = new SimpleUlidRng();
                    for (int i = 0; i < it; i++)
                        Ulid.NewUlid(simplerng);
                }, 
                iterations,
                "Ulid.NewUlid(SimpleUlidRng):                                  {0,15:N0}/sec"
            );

            Time<object>(
                (p, it) =>
                {
                    var csrng = new CSUlidRng();
                    for (int i = 0; i < it; i++)
                        Ulid.NewUlid(csrng);
                },
                iterations,
                "Ulid.NewUlid(CSUlidRng):                                      {0,15:N0}/sec"
            );

            Time(
                (it) => { return Enumerable.Range(0, it).Select(n => Guid.NewGuid().ToString()); },
                (p, it) =>
                {
                    foreach (var i in p)
                        Guid.Parse(i);
                },
                iterations,
                "Guid.Parse(string):                                           {0,15:N0}/sec"
            );

            Time(
                (it) => { return Enumerable.Range(0, it).Select(n => Ulid.NewUlid().ToString()); },
                (p, it) =>
                {
                    foreach (var i in p)
                        Ulid.Parse(i);
                }, 
                iterations,
                "Ulid.Parse(string):                                           {0,15:N0}/sec"
            );


            Time(
                (it) => { return Enumerable.Range(0, it).Select(n => Guid.NewGuid()); },
                (p, it) =>
                {
                    foreach (var i in p)
                        i.ToString();
                },
                iterations,
                "Guid.ToString():                                              {0,15:N0}/sec"
            );

            Time(
                (it) => { return Enumerable.Range(0, it).Select(n => Ulid.NewUlid()); },
                (p, it) =>
                {
                    foreach (var i in p)
                        i.ToString();
                },
                iterations,
                "Ulid.ToString():                                              {0,15:N0}/sec"
            );

            Time(
                (it) => {
                    var r = new Random();
                    return Enumerable.Range(0, it).Select(n => { var b = new byte[16]; r.NextBytes(b); return b; });
                },
                (p, it) =>
                {
                    foreach (var i in p)
                        new Guid(i);
                },
                iterations,
                "new Guid(byte[]):                                             {0,15:N0}/sec"
            );

            Time(
                (it) => {
                    var r = new Random();
                    return Enumerable.Range(0, it).Select(n => { var b = new byte[16]; r.NextBytes(b); return b; });
                },
                (p, it) =>
                {
                    foreach (var i in p)
                        new Ulid(i);
                },
                iterations,
                "new Ulid(byte[]):                                             {0,15:N0}/sec"
            );

            Time(
                (it) => { return Enumerable.Range(0, it).Select(n => Guid.NewGuid()); },
                (p, it) =>
                {
                    foreach (var i in p)
                        i.ToByteArray();
                },
                iterations,
                "Guid.ToByteArray():                                           {0,15:N0}/sec"
            );

            Time(
                (it) => { return Enumerable.Range(0, it).Select(n => Ulid.NewUlid()); },
                (p, it) =>
                {
                    foreach (var i in p)
                        i.ToByteArray();
                },
                iterations,
                "Ulid.ToByteArray():                                           {0,15:N0}/sec"
            );

            Time(
                (it) => { return Enumerable.Range(0, it).Select(n => Ulid.NewUlid()); },
                (p, it) =>
                {
                    foreach (var i in p)
                        i.ToGuid();
                },
                iterations,
                "Ulid.ToGuid():                                                {0,15:N0}/sec"
            );

            Time(
                (it) => { return Enumerable.Range(0, it).Select(n => Guid.NewGuid()); },
                (p, it) =>
                {
                    foreach (var i in p)
                        new Ulid(i);
                },
                iterations,
                "new Ulid(Guid):                                               {0,15:N0}/sec"
            );

            Console.ReadKey();
        }

        private static double Time<T>(Action<T, int> act, int iterations, string description)
        {
            return Time(null, act, iterations, description);
        }

        private static double Time<T>(Func<int, T> prepare, Action<T, int> act, int iterations, string description)
        {
            T prepared = default(T);
            if (prepare != null)
                prepared = prepare(iterations);

            //Warmup
            act(prepared, 5);

            var s = Stopwatch.StartNew();
            act(prepared, iterations);
            var e = s.Elapsed;

            var result = (iterations / e.TotalMilliseconds) * 1000;
            Console.WriteLine(description, result);

            return result;
        }
    }
}
