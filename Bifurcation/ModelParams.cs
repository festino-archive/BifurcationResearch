using System.Numerics;

namespace Bifurcation
{
    public struct ModelParams
    {
        public Complex A0;
        public double D, K, A0m2;
        public double[] u0;

        public ModelParams(Complex A0, double K, double[] u0, double D = 0.01)
        {
            this.A0 = A0;
            A0m2 = A0.Magnitude * A0.Magnitude;
            this.D = D;
            this.K = K;
            this.u0 = u0;
        }
    }
}
