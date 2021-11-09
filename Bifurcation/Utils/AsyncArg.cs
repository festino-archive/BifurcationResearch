using System;
using System.Threading;

namespace Bifurcation
{
    internal class AsyncArg
    {
        public readonly IProgress<double> Progress;
        public readonly CancellationToken Token;

        public AsyncArg(IProgress<double> progress, CancellationToken token)
        {
            Progress = progress;
            Token = token;
        }
    }
}
