using System;
using System.Threading;

namespace Bifurcation
{
    internal class AsyncArg
    {
        public readonly Solver.Method Method;
        public readonly IProgress<double> CalcProgress;
        public readonly CancellationToken Token;

        public AsyncArg(Solver.Method method, IProgress<double> calcProgress, CancellationToken token)
        {
            Method = method;
            CalcProgress = calcProgress;
            Token = token;
        }
    }
}
