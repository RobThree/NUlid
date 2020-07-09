using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NUlid.Performance.Benchmarking
{
    public class ForEachBench<T> : Bench
    {
        public Action<T> Action { get; private set; }
        public Func<int, IEnumerable<T>> Prepare { get; private set; }

        public ForEachBench(string title, Action<T> action, Func<int, IEnumerable<T>> prepare, int? iterations = null, int? warmuprounds = null)
            : base(title, iterations, warmuprounds)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            Prepare = prepare ?? throw new ArgumentNullException(nameof(prepare));
        }

        public override void Execute(int iterations)
        {
            Parallel.ForEach(Prepare(iterations), (p) => Action(p));
        }
    }
}