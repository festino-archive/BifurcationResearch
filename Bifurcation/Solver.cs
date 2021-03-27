using System;
using System.Numerics;

namespace Bifurcation
{
    class Solver
    {

        // change if param was changed
        private bool Solved { get; set; }

        public ModelParams Parameters;
        public Complex A0;
        public double D, K, A0m2;
        int N, M;
        double[] u0;
        public double T;
        double xStep, tStep;
        Filter P;
        public int FilterFullSize { get; private set; }
        public int FilterSize { get; private set; }
        public double Chi { get; private set; }
        public double[,] Solution { get; private set; }
        public int TSize { get => M; }
        public int XSize { get => N; }

        public Solver(Filter P, double T, int xCount = 200, int timeCount = 1000)
        {
            this.P = P;
            FilterFullSize = P.FullSize;
            FilterSize = P.Size;
            N = xCount;
            M = timeCount;
            this.T = T;
            xStep = 2 * Math.PI / N;
            tStep = T / (M - 1);
            if (tStep > xStep * xStep / 2 / D)
            {
                throw new Exception($"stability error: h={xStep:f4}, τ={tStep:f4}, 2Dτ/h^2={2 * D * tStep / (xStep * xStep):f4} > 1");
            }
        }

        public void SetParams(ModelParams p)
        {
            Parameters = p;
            K = p.K;
            A0 = p.A0;
            A0m2 = p.A0m2;
            D = p.D;
            u0 = p.u0;
        }

        public static double GetChi(double K, Complex pho00, Complex A0)
        {
            double A0m = A0.Magnitude;
            double p = (1 + pho00).Magnitude;
            return K * A0m * A0m * p * p; // (20)
        }

        public double GetX(int j) => j * xStep;
        public double GetT(int k) => k * tStep;

        public void Solve(AsyncArg asyncArg)
        {
            if (Solved)
                return;
            Solved = true;

            int P_max = FilterSize;
            Chi = GetChi(K, P[P_max, P_max], A0);

            Solution = new double[M, N];
            double[,] u = Solution;
            for (int j = 0; j < N; j++)
            {
                double p = j / (float)N * u0.Length;
                double low = Math.Floor(p);
                double high = Math.Ceiling(p);
                double lowWeight = 1 - (p - low);
                double highWeight = 1 - lowWeight;
                if (high >= u0.Length)
                    high = 0;
                double weightSum = lowWeight * u0[(int)low] + highWeight * u0[(int)high];
                u[0, j] = weightSum;
            }
            double k1 = D / xStep / xStep;
            double kMax = M - 2;
            for (int k = 0; k < M - 1; k++)
            {
                if (asyncArg.token.IsCancellationRequested)
                    return;

                Complex[] filtered = P.Apply(A0, N, u, k);

                double[] iFT = new double[N];
                for (int j = 0; j < N; j++)
                {
                    iFT[j] = filtered[j].Magnitude;
                }

                for (int j = 0; j < N; j++)
                {
                    int left = j - 1;
                    if (left < 0)
                        left = N - 1;
                    int right = j + 1;
                    if (right >= N)
                        right = 0;
                    double Duxx = k1 * (u[k, right] - 2 * u[k, j] + u[k, left]);

                    double F = K * iFT[j] * iFT[j];
                    u[k + 1, j] = u[k, j] + tStep * (Duxx - u[k, j] + F);
                }

                asyncArg.calcProgress?.Report((k + 1) / kMax);
            }
        }
    }
}
