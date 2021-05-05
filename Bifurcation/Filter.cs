using System;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Factorization;
using MathNet.Numerics.IntegralTransforms;
using System.Collections.Generic;

namespace Bifurcation
{
    class Filter
    {
        private Complex[,] P;
        public int Size { get; private set; }
        public int FullSize { get; private set; }

        public bool IsDiagonal { get; private set; }
        private int NonDiagonalCount;
        private List<Tuple<int, int>> NonZeroElements = new List<Tuple<int, int>>();
        private bool UseNonZero = true;

        private Complex[] alphas = null;
        private Complex[] betas = null;
        private Complex[,] gammas = null;

        public Filter(int size)
        {
            Size = size;
            FullSize = Size * 2 + 1;
            P = new Complex[FullSize, FullSize];
            NonDiagonalCount = 0;
            UpdateNonZeroBools();
        }

        public Filter(Complex[,] P)
        {
            if (P.GetLength(0) != P.GetLength(1))
                throw new Exception("non-square matrix P");
            this.P = (Complex[,])P.Clone();
            FullSize = P.GetLength(0);
            Size = (FullSize - 1) / 2;
            ScanElements(P);
        }

        private void ScanElements(Complex[,] P)
        {
            int count = 0;
            for (int i = 0; i < FullSize; i++)
                for (int j = 0; j < FullSize; j++)
                    if (P[i, j] != 0)
                    {
                        NonZeroElements.Add(Tuple.Create(i, j));
                        if (i != j)
                            count++;
                    }
            NonDiagonalCount = count;
            UpdateNonZeroBools();
        }

        private void UpdateNonZeroBools()
        {
            IsDiagonal = NonDiagonalCount == 0;
            UseNonZero = NonZeroElements.Count / (double)(FullSize * FullSize) <= 0.5;
        }

        public Complex this[int i, int j]
        {
            get => P[i, j];
            set
            {
                if (P[i, j] == 0 && value != 0)
                {
                    NonZeroElements.Add(Tuple.Create(i, j));
                    NonZeroElements.Sort();
                    if (i != j)
                        NonDiagonalCount++;
                }
                else if (P[i, j] != 0 && value == 0)
                {
                    NonZeroElements.Remove(Tuple.Create(i, j));
                    if (i != j)
                        NonDiagonalCount--;
                }
                UpdateNonZeroBools();
                if (P[i, j] != value)
                {
                    alphas = null;
                    betas = null;
                    gammas = null;
                }
                P[i, j] = value;
            }
        }

        public Complex[] Apply(Complex A_in, int N, double[,] u, int k)
        {
            Complex[] filtered = new Complex[N];
            Complex[] FFT = new Complex[N];
            for (int j = 0; j < N; j++)
            {
                FFT[j] = new Complex(Math.Cos(u[k, j]), Math.Sin(u[k, j]));
                FFT[j] *= A_in;
                filtered[j] = FFT[j];
            }

            Fourier.Forward(FFT, FourierOptions.NoScaling);
            double norm = 1.0 / N;
            for (int j = 0; j < N; j++)
                FFT[j] = norm * FFT[j];

            if (UseNonZero)
                ApplyNonZero(filtered, FFT, N);
            else
                ApplyFull(filtered, FFT, N);

            return filtered;
        }

        public Complex[] Apply(Complex A_in, int N, double[] u)
        {
            Complex[] filtered = new Complex[N];
            Complex[] FFT = new Complex[N];
            for (int j = 0; j < N; j++)
            {
                FFT[j] = new Complex(Math.Cos(u[j]), Math.Sin(u[j]));
                FFT[j] *= A_in;
                filtered[j] = FFT[j];
            }

            Fourier.Forward(FFT, FourierOptions.NoScaling);
            double norm = 1.0 / N;
            for (int j = 0; j < N; j++)
                FFT[j] = norm * FFT[j];

            if (UseNonZero)
                ApplyNonZero(filtered, FFT, N);
            else
                ApplyFull(filtered, FFT, N);

            return filtered;
        }

        private void ApplyNonZero(Complex[] filtered, Complex[] FFT, int N)
        {
            foreach ((int l, int j) in NonZeroElements)
            {
                for (int m = 0; m < N; m++)
                {
                    Complex w = e_k((l - Size) * m, N); // new w for same l and different j
                    if (j < Size)
                        filtered[m] += P[l, j] * FFT[FFT.Length - Size + j] * w;
                    else
                        filtered[m] += P[l, j] * FFT[j - Size] * w;
                }
            }
        }

        private void ApplyFull(Complex[] filtered, Complex[] FFT, int N)
        {
            for (int m = 0; m < N; m++)
            {
                for (int l = 0; l < FullSize; l++)
                {
                    Complex w = e_k((l - Size) * m, N);
                    for (int j = 0; j < Size; j++)
                    {
                        filtered[m] += P[l, j] * FFT[FFT.Length - Size + j] * w;
                    }
                    for (int j = Size; j < FullSize; j++)
                    {
                        filtered[m] += P[l, j] * FFT[j - Size] * w;
                    }
                }
            }
        }

        private static Complex e_k(int k, int N)
        {
            if (k % N == 0) return 1;
            double arg = 2 * Math.PI * k / N;
            return new Complex(Math.Cos(arg), Math.Sin(arg));
        }

        public Tuple<Complex[], Complex[,]> GetEigenValues(ModelParams p)
        {
            CalcABG();
            int fullSize = FullSize;
            int N = Size;
            Complex[,] system = new Complex[fullSize, fullSize];
            for (int m = -N; m <= N; m++)
            {
                int M = N + m;
                int nM = N - m;
                Complex delta = new Complex(0, p.K * p.A0m2);
                system[M, M] += -(1 + p.D * m * m) + delta * alphas[M];
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
            for (int n = 0; n < fullSize; n++)
            {
                double length2 = 0;
                for (int m = 0; m < fullSize; m++)
                    length2 += vectors[m, n].Magnitude * vectors[m, n].Magnitude;
                double norm = 1 / Math.Sqrt(length2);
                for (int m = 0; m < fullSize; m++)
                    vectors[m, n] *= norm;
            }
            return Tuple.Create(values, vectors);
        }

        public Complex GetDerivative(int eigen, ModelParams p)
        {
            int fullSize = FullSize;
            int N = Size;
            if (eigen < -N || eigen > N)
                return 0;
            var eigenVal = GetEigenValues(p);
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
            der *= p.A0m2;
            return der;
        }

        private void CalcABG()
        {
            if (alphas != null)
                return;
            int fullSize = FullSize;
            int N = Size;
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

        public int FindDiagCriticalN(double D, double A0, double K)
        {
            bool found = false;
            int n_cap = 0;
            double speed = 0;
            for (int n = 0; n <= Size; n++)
            {
                Complex pn = P[Size + n, Size + n];
                Complex p_n = P[Size - n, Size - n];
                double re = -K * A0 * A0 * (pn.Imaginary + p_n.Imaginary) - 1 - D * n * n;
                if (re > 0)
                {
                    found = true;
                    n_cap = n;
                    double im = K * A0 * A0 * (pn.Real - p_n.Real);
                    if (im < 0)
                        n_cap = -3;
                    speed = -A0 * A0 * (pn.Imaginary + p_n.Imaginary);
                    break;
                }
            }
            if (!found)
            {
                Logger.Write("No critical n^");
                return 0;
            }
            for (int n = 0; n <= Size; n++)
            {
                if (n == n_cap || n == -n_cap)
                    continue;
                Complex p_n = P[Size - n, Size - n];
                Complex pn = P[Size + n, Size + n];
                double re = -K * A0 * A0 * (pn.Imaginary + p_n.Imaginary) - 1 - D * n * n;
                if (re > 0)
                {
                    Logger.Write($"Second \"critical\": {n_cap} and {n}");
                    return 0;
                }
            }
            string state = "non-positive";
            if (speed > 0)
                state = "positive";
            Logger.Write($"Derivative of {n_cap} is {state}: {speed}");

            return n_cap;
        }
        public double FindDiagCritical(double D, double A0, int n_cap)
        {
            Complex p_n = P[Size - n_cap, Size - n_cap];
            Complex pn = P[Size + n_cap, Size + n_cap];
            double divider = - A0 * A0 * (pn.Imaginary + p_n.Imaginary);
            double eps = 0.01;
            if (-eps < divider && divider < eps)
                if (divider < 0)
                    return double.NegativeInfinity;
                else
                    return double.PositiveInfinity;
            return (1 + D * n_cap * n_cap) / divider;
        }
    }
}
