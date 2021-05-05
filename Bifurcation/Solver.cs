using MathNet.Numerics.LinearAlgebra;
using System;
using System.Numerics;

namespace Bifurcation
{
    class Solver
    {
        public enum Method
        {
            EXPLICIT, IMPLICIT_2
        }
        private static readonly int MAX_ITER = 10;

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
        }

        public void SetParams(ModelParams p)
        {
            Parameters = p;
            K = p.K;
            A0 = p.A0;
            A0m2 = p.A0m2;
            D = p.D;
            u0 = p.u0;
            Solved = false;
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

            if (asyncArg.Method == Method.EXPLICIT)
                SolveExplicit(asyncArg);
            else if (asyncArg.Method == Method.IMPLICIT_2)
                SolveImplicit(asyncArg);
        }
        
        private void SolveExplicit(AsyncArg asyncArg)
        {
            double sigma = D * tStep / (xStep * xStep);
            if (sigma > 0.5)
            {
                throw new Exception($"stability error: h={xStep:f4}, τ={tStep:f4}, 2Dτ/h^2={sigma:f4} > 1/2");
            }

            double[,] u = Solution;

            double k1 = D / xStep / xStep;
            double kMax = M - 2;
            for (int k = 0; k < M - 1; k++)
            {
                if (asyncArg.Token.IsCancellationRequested)
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

                asyncArg.CalcProgress?.Report((k + 1) / kMax);
            }
        }

        private void SolveImplicit(AsyncArg asyncArg)
        {
            /*if (tStep > 0.5)
            {
                throw new Exception($"convergence error: τ={tStep:f4} > 0.5");
            }*/

            var Vec = MathNet.Numerics.LinearAlgebra.Vector<double>.Build;
            var Mat = Matrix<double>.Build;
            double[,] u = Solution;

            double sigma = 0.5 * D * tStep / (xStep * xStep);
            double[,] rawA = new double[N, N];
            for (int i = 0; i < N; i++)
                rawA[i, i] = 1 + 2 * sigma + tStep * 0.5;
            for (int i = 1; i < N; i++)
                rawA[i - 1, i] = rawA[i, i - 1] = -sigma;
            rawA[0, N - 1] = rawA[N - 1, 0] = -sigma;
            Matrix<double> A = Mat.DenseOfArray(rawA);
            Matrix<double> AInverted = A.Inverse();

            double[,] rawB = rawA;
            for (int i = 0; i < N; i++)
                rawB[i, i] = 1 - 2 * sigma - tStep * 0.5;
            for (int i = 1; i < N; i++)
                rawB[i - 1, i] = rawB[i, i - 1] = sigma;
            rawB[0, N - 1] = rawB[N - 1, 0] = sigma;
            Matrix<double> B = Mat.DenseOfArray(rawB);

            Matrix<double> AinvB = AInverted * B;
            Matrix<double> tauAinv = tStep * AInverted;

            double[] u0 = new double[N];
            for (int j = 0; j < N; j++)
                u0[j] = u[0, j];
            var u_k = Vec.DenseOfArray(u0);

            double kMax = M - 2;
            for (int k = 0; k < M - 1; k++)
            {
                if (asyncArg.Token.IsCancellationRequested)
                    return;

                var u_kl = u_k.Clone();
                var U = AinvB * u_k;
                u_k = 0.5 * u_k;
                for (int l = 0; l < MAX_ITER; l++)
                {
                    Complex[] filtered = P.Apply(A0, N, (u_k + 0.5 * u_kl).ToArray());

                    double[] iFT = new double[N];
                    for (int j = 0; j < N; j++)
                    {
                        iFT[j] = filtered[j].Magnitude;
                        iFT[j] = K * iFT[j] * iFT[j];
                    }
                    var FP = Vec.DenseOfArray(iFT);
                    //u_k = invAB * u_k + tauA * FP;
                    u_kl = U + tauAinv * FP;

                    // stop cond
                }
                u_k = u_kl;

                for (int j = 0; j < N; j++)
                    u[k + 1, j] = u_k[j];

                asyncArg.CalcProgress?.Report((k + 1) / kMax);
            }
        }
    }
}
