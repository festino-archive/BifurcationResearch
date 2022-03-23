using System;
using System.Numerics;
using System.Windows.Controls;

namespace Bifurcation
{
    class FilterInfo
    {
        public static Tuple<Complex[], Complex[,]> UpdateEigen(Filter filter, TextBlock textBlock, ModelParams param)
        {
            var eigen = filter.GetEigenValues(param);
            Logger.Write("eigenvalues:");
            Logger.Write(eigen.Item1);
            Logger.Write("eigenvectors: (columns)");
            Logger.Write(eigen.Item2);

            if (filter.IsDiagonal)
            {
                int n_cap = filter.FindDiagCriticalN(param.D, 1, param.K);
                textBlock.Text = $"K^({n_cap}) = " + filter.FindDiagCritical(param.D, 1, n_cap);
                textBlock.Text += '\n' + "n^ = " + n_cap;
            }
            else
            {
                int count = 0;
                int[] n_cap = new int[eigen.Item1.Length];
                bool multi = false;
                Complex value = 0;
                for (int n = 0; n < eigen.Item1.Length; n++)
                {
                    Complex v = eigen.Item1[n];
                    if (v.Real > -0.001)
                    {
                        if (Math.Abs(value.Imaginary - v.Imaginary) >= 0.001)
                            multi = true;
                        n_cap[count] = n;
                        count++;
                        value = v;
                    }
                }
                if (count == 0)
                    textBlock.Text = "No n^";
                else if (multi)
                    textBlock.Text = "Multi n^";
                else
                {
                    string text = "λn^ = " + value.ToString("f3") + " (multiplicity=" + count;
                    if (count == 1)
                        text += ", derivative=" + filter.GetDerivative(n_cap[0], param).ToString("f2") + ")";
                    else
                    {
                        text += ")\nderivatives:[";
                        for (int i = 0; i < count; i++)
                        {
                            if (i > 0)
                                text += ", ";
                            text += filter.GetDerivative(n_cap[i], param).ToString("f2");
                        }
                        text += "]";
                    }
                    textBlock.Text = text;
                }
            }
            return eigen;
        }
    }
}
