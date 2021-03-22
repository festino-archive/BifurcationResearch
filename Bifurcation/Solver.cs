using System;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.IntegralTransforms;

namespace Bifurcation
{
    class Solver
    {

        // change if param was changed
        private bool Solved { get; set; }
        private Complex[] alphas = null;
        private Complex[] betas = null;
        private Complex[,] gammas = null;

        public Complex A0;
        public double D, T, K, A0m2;
        int N, M;
        double[] u0;
        double xStep, tStep;
        Complex[,] P;
        public double Chi { get; private set; }
        public double[,] Solution { get; private set; }
        public int TSize { get => M; }
        public int XSize { get => N; }

        public Solver(Complex[,] P, double T, int xCount = 200, int timeCount = 1000)
        {
            if (P.GetLength(0) == P.GetLength(1))
                this.P = P;
            else
                throw new Exception("non-square matrix P");
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

        public void SetParams(Complex A0, double K, double[] u0, double D = 0.01)
        {
            this.A0 = A0;
            A0m2 = A0.Magnitude * A0.Magnitude;
            this.D = D;
            this.K = K;
            this.u0 = u0;
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

            int P_size = P.GetLength(0);
            int P_max = (P_size - 1) / 2;
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

        private void CalcABG()
        {
            if (alphas != null)
                return;
            int fullSize = P.GetLength(0);
            int N = (fullSize - 1) / 2;
            alphas = new Complex[fullSize];
            betas = new Complex[fullSize];
            gammas = new Complex[fullSize, fullSize];
            Complex rho = ComplexUtils.GetQ(P[N, N]);
            Complex rho_c = ComplexUtils.GetConjugateQ(P[N, N]);
            for (int m = -N; m <= N; m++)
            {
                int M = N + m;
                int nM = N - m;
                alphas[M] = rho_c * ComplexUtils.GetQ(P[M, M]) - rho * ComplexUtils.GetConjugateQ(P[nM, nM]);
                betas[M] = rho_c * P[nM, M] - rho * ComplexUtils.GetConjugate(P[M, nM]);
                for (int k = -N; k <= N; k++)
                {
                    if (k == m || k == -m)
                        continue;
                    int K = N + k;
                    int nK = N - k;
                    gammas[M, K] = rho_c * P[M, K] - rho * ComplexUtils.GetConjugate(P[nM, nK]);
                }
            }
        }

        public Tuple<Complex[], Complex[,]> GetEigenValues()
        {
            CalcABG();
            int fullSize = P.GetLength(0);
            int N = (fullSize - 1) / 2;
            Complex[,] system = new Complex[fullSize, fullSize];
            for (int m = -N; m <= N; m++)
            {
                int M = N + m;
                int nM = N - m;
                Complex delta = new Complex(0, K * A0m2);
                system[M, M] += -(1 + D * m * m) + delta * alphas[M];
                system[M, nM] += delta * betas[nM];
                for (int k = -N; k <= N; k++)
                {
                    if (k == m || k == -m)
                        continue;
                    int K = N + k;
                    system[M, K] += delta * gammas[M, K];  
                }
            }
            Matrix<Complex> mathSystem = Matrix<Complex>.Build.DenseOfArray(system);
            Evd<Complex> eigen = mathSystem.Evd();
            Complex[] values = eigen.EigenValues.ToArray();
            Complex[,] vectors = eigen.EigenVectors.ToArray();
            return Tuple.Create(values, vectors);
        }

        public Complex GetDerivative(int eigen)
        {
            int fullSize = P.GetLength(0);
            int N = (fullSize - 1) / 2;
            if (eigen < -N || eigen > N)
                return 0;
            var eigenVal = GetEigenValues();
            Complex der = 0;
            for (int m = -N; m <= N; m++)
            {
                int M = N + m;
                int nM = N - m;
                der += alphas[M] * eigenVal.Item2[M, eigen];
                der += betas[nM] * eigenVal.Item2[nM, eigen];
                for (int k = -N; k <= N; k++)
                {
                    if (k == m || k == -m)
                        continue;
                    int K = N + k;
                    der += gammas[M, K] * eigenVal.Item2[K, eigen];
                }
            }
            der /= fullSize;
            der *= A0m2;
            return der;
        }

        private static Complex e_k(int k, int N)
        {
            if (k % N == 0) return 1;
            double arg = 2 * Math.PI * k / N;
            return new Complex(Math.Cos(arg), Math.Sin(arg));
        }
    }
}
