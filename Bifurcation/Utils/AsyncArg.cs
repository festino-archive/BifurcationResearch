using System;
using System.Threading;

namespace Bifurcation
{
    internal class AsyncArg
    {
        public readonly Solver.Method Method;
        public readonly IProgress<double> CalcProgress;
        public readonly IProgress<double> DrawProgress;
        public readonly CancellationToken Token;

        public AsyncArg(Solver.Method method, IProgress<double> calcProgress, IProgress<double> drawProgress, CancellationToken token)
        {
            Method = method;
            CalcProgress = calcProgress;
            DrawProgress = drawProgress;
            Token = token;
        }
    }
}
