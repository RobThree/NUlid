using System;
using System.Threading.Tasks;

namespace NUlid.Performance.Benchmarking
{
    public class ForBench : Bench
    {
        public Action Action { get; private set; }
        public ForBench(string title, Action action, int? iterations = null, int? warmuprounds = null)
            : base(title, iterations, warmuprounds)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public override void Execute(int iterations)
        {
            Parallel.For(0, iterations, (i) => Action());
        }
    }
}