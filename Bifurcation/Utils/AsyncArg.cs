using System;
using System.Threading;

namespace Bifurcation
{
    internal class AsyncArg
    {
        public IProgress<double> calcProgress;
        public IProgress<double> drawProgress;
        public CancellationToken token;

        public AsyncArg(IProgress<double> calcProgress, IProgress<double> drawProgress, CancellationToken token)
        {
            this.calcProgress = calcProgress;
            this.drawProgress = drawProgress;
            this.token = token;
        }
    }
}
