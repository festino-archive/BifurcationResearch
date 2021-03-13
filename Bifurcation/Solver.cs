using MathNet.Numerics.IntegralTransforms;
using System;
using System.Numerics;

namespace Bifurcation
{
    class Solver
    {
        private static readonly double A0 = 1;

        // change if param was changed
        private bool Solved { get; set; }

        public double D, T, K;
        int N, M;
        double[] u0;
        double xStep, tStep;
        Complex[,] P;
        public double Chi { get; private set; }
        public double[,] Solution { get; private set; }
        public int TSize { get => M; }
        public int XSize { get => N; }

        public Solver(Complex[,] P, double K, double[] u0, double T, double D = 0.01f, int xCount = 200, int timeCount = 1000)
        {
            if (P.GetLength(0) == P.GetLength(1))
                this.P = P;
            else
                throw new Exception("non-square matrix P");
            this.D = D;
            this.T = T;
            N = xCount;
            M = timeCount;
            xStep = 2 * Math.PI / N;
            tStep = T / (M - 1);
            if (tStep > xStep * xStep / 2 / D)
            {
                throw new Exception($"stability error: h={xStep:f4}, τ={tStep:f4}, 2Dτ/h^2={2 * D * tStep / (xStep * xStep):f4} > 1");
            }
            this.K = K;
            this.u0 = u0;
        }

        public static double GetChi(double K, Complex pho00)
        {
            double p = (1 + pho00).Magnitude;
            return K * A0 * A0 * p * p; // (20)
        }

        public double GetX(int j) => j * xStep;
        public double GetT(int k) => k * tStep;

        public void Solve(AsyncArg asyncArg)
        {
            if (Solved)
                return;
            Solved = true;

            int P_size = P.GetLength(0);
            int P_max = (P_size - 1) / 2;
            Chi = GetChi(K, P[P_max, P_max]);

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

                Complex[] filtered = new Complex[N];
                Complex[] FFT = new Complex[N];
                for (int j = 0; j < N; j++)
                {
                    FFT[j] = new Complex(Math.Cos(u[k, j]), Math.Sin(u[k, j]));
                    FFT[j] *= A0;
                    filtered[j] = FFT[j];
                }

                Fourier.Forward(FFT, FourierOptions.NoScaling);
                double norm = 1.0 / N;
                for (int j = 0; j < N; j++)
                    FFT[j] = norm * FFT[j];

                //if (k == 0)
                //    Logger.Write(FFT);

                for (int m = 0; m < N; m++)
                {
                    for (int l = 0; l < P_size; l++)
                    {
                        Complex w = e_k((l - P_max) * m, N);
                        for (int j = 0; j < P_max; j++)
                        {
                            filtered[m] += P[l, j] * FFT[FFT.Length - P_max + j] * w;
                        }
                        for (int j = P_max; j < P_size; j++)
                        {
                            filtered[m] += P[l, j] * FFT[j - P_max] * w;
                        }
                    }
                }
                double[] iFT = new double[N];
                for (int j = 0; j < N; j++)
                {
                    iFT[j] = filtered[j].Magnitude;
                }
                //if (k == 0)
                //    Logger.Write(filtered);

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

        private static Complex e_k(int k, int N)
        {
            if (k % N == 0) return 1;
            double arg = 2 * Math.PI * k / N;
            return new Complex(Math.Cos(arg), Math.Sin(arg));
        }
    }
}
