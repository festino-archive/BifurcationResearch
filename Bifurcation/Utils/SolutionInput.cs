using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Numerics;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Bifurcation
{
    internal class StringProperty
    {
        public string Name { get; private set; }
        public string Value { get; set; }

        public StringProperty(string name, string val)
        {
            Name = name;
            Value = val;
        }

        public static implicit operator string(StringProperty sp) => sp.Value;
    }

    internal class SolutionInput
    {
        //default values
        public StringProperty D { get; private set; } = new StringProperty("D", "0.01");
        public StringProperty K { get; private set; } = new StringProperty("K", "3.2");
        public StringProperty A_0 { get; private set; } = new StringProperty("A_0", "1");
        public StringProperty T { get; private set; } = new StringProperty("T", "120");
        public StringProperty t_count { get; private set; } = new StringProperty("t_count", "5000");
        public StringProperty x_count { get; private set; } = new StringProperty("x_count", "256");
        public StringProperty u_0 { get; private set; } = new StringProperty("u_0", "chi + 0.1 cos 2.5x");
        public StringProperty v { get; private set; } = new StringProperty("v", "chi + cos (3x - 0.1t)");
        private readonly List<StringProperty> Properties = new List<StringProperty>();

        public bool IsFilterGrid { get; private set; }
        public Complex[,] FilterGrid { get; private set; }
        public string FilterFormulas { get; private set; }
        public FilterBuilder FilterBuilder { get; private set; }
        public Filter Filter { get => FilterBuilder.Filter; }

        public SolutionInput()
        {
            Properties.AddRange(new StringProperty[] { D, K, A_0, T, t_count, x_count, u_0, v });

            /*Complex[,] P = new Complex[11, 11];
            P[0, 0] = new Complex(0.2, -0.3);
            P[10, 10] = new Complex(0.6, 0.3);
            P[2, 2] = new Complex(0.4, -0.159);
            P[8, 8] = new Complex(-0.4, -0.159);*/

            /*Complex[,] P = new Complex[41, 41];
            P[20 + 1, 20 + 1] = new Complex(-0.1, -0.1 / 3);
            P[20 - 1, 20 - 1] = new Complex(-0.1, 0);
            P[20 + 1, 20 - 1] = new Complex(-0.1, -0.3);
            P[20 - 1, 20 + 1] = new Complex(-0.1, 0);
            for (int i = 2; i <= 10; i++)
            {
                int n = 2 * i - 1;
                double g = (1 + double.Parse(D.Value) * n * n) / (double.Parse(K.Value) * 1 * 1) / n;
                if (i % 2 == 1)
                    g *= -1;
                //P[20 + n, 20+1] = new Complex(0, g / 4);
                //P[20 + n, 20-1] = new Complex(0, g / 4);
                //P[20 - n, 20+1] = new Complex(0, g / 4);
                //P[20 - n, 20-1] = new Complex(0, g / 4);
                P[20 + n, 20+1] = new Complex(0, g);
            }

            FilterGrid = P;
            IsFilterGrid = true;

            string expected = "chi";
            for (int i = 1; i <= 10; i++)
            {
                int n = 2 * i - 1;
                double g = 0.448 / n;
                if (i % 2 == 0)
                    g *= -1;

                expected += " ";
                if (g > 0)
                    expected += "+";
                expected += $"{g} cos({n}x)";
            }
            v.Value = expected;*/

            /*
            // turing halfs
            Func<int, double> f0 = (n) => 0;
            Func<int, double> f = (n) => (n % 2) * (1 - 2 * (n % 4 / 2)) / (double)(n);
            InitTuring(4, f, f0, new Complex(0.2, -0.2), 20, 1);*/

            /*
            // hopf blinking halfs
            Func<int, double> f0 = (n) => 0;
            Func<int, double> fn0 = (n) => n == 1 ? 0.1 : 0;
            Func<int, double> f = (n) => (n % 2) * (1 - 2 * (n % 4 / 2)) / (double)(n);
            InitHopf(4, f, f0, f0, fn0, 20, 1);*/

            /*
            // central peaks
            Func<int, double> f0 = (n) => 0;
            Func<int, double> fn0 = (n) => n == 1 ? 0.01 : 0;
            Func<int, double> f = (n) => (1 - 2 * (n % 2)) / (double)(n);
            InitHopf(4, f, f0, f0, fn0, 10, 1);*/

            /*
            // turing spikes
            Func<int, double> f0 = (n) => 0;
            Func<int, double> fn0 = (n) => n == 1 ? 0.005 : 0;
            Func<int, double> f = (n) => ( n == 1 ? -0.005 : 0 + (1 - (n % 4) / 2) * (1 - 2 * (n % 2))) / (double)(n);
            InitHopf(4, f, f0, f0, fn0, 20, 1);
            */

            /*
            // turing rolls
            Func<int, double> f0 = (n) => 0;
            Func<int, double> fn0 = (n) => n == 1 ? 0.995 : 0;
            Func<int, double> f = (n) => (n == 1 ? 0.005 : 0 + (1 - (n % 4) / 2) * (1 - 2 * (n % 2))) / (double)(n);
            InitHopf(4, f, f0, f0, fn0, 20, 1);
            */

            /*
            // central drifting roll
            Func<int, double> f = (n) => (1 - 2 * (n % 2)) / (double)(n * n);
            Func<int, double> mf = (n) => -(1 - 2 * (n % 2)) / (double)(n * n);
            InitHopf(4, f, f, f, mf);*/

            /*
            // square to triangle
            Func<int, double> f0 = (n) => 0;
            Func<int, double> fn0 = (n) => n == 1 ? 0.1 : 0;
            Func<int, double> a = (n) => (n == 1 ? -0.1 : 0) + 2 * (n % 2) * (1 - 2 * (n % 4 / 2)) / (double)(n);
            Func<int, double> c = (n) => 0.8 * 2 * (n % 2) / (double)(n * n);
            a = (n) => 0.5 * 2 * (n % 2) / (double)(n * n);
            c = (n) => (n == 1 ? -0.1 : 0) + 2 * (n % 2) * (1 - 2 * (n % 4 / 2)) / (double)(n);
            InitHopf(4, a, f0, c, fn0, 20);*/

            /*
            // Blinking triangles
            Func<int, double> f0 = (n) => 0;
            Func<int, double> fn0 = (n) => n == 1 ? 0.26 : 0;
            Func<int, double> a = (n) => 0.8 * 2 * (n % 2) / (double)(n * n);
            Func<int, double> c = (n) => 0.8 * 2 * (n % 2) / (double)(n * n);
            InitHopf(4, a, f0, c, fn0, 20);
            */
        }

        private void InitHopf(double K_hat, Func<int, double> a_n, Func<int, double> b_n, Func<int, double> c_n, Func<int, double> d_n, int count = 10, int step = 1, double expAmpl = 0.125)
        {
            int main = step;
            K.Value = K_hat.ToString();
            double c_hat = K_hat * 1 * 1;
            int s = 1;
            Complex x_N = new Complex(a_n(s) + d_n(s), c_n(s) - b_n(s)) * 0.5;
            Complex x_mN = new Complex(a_n(s) - d_n(s), c_n(s) + b_n(s)) * 0.5;
            double omega = 1;
            Complex x_N2 = x_N * Complex.Conjugate(x_N);
            Complex x_mN2 = x_mN * Complex.Conjugate(x_mN);
            Complex beta = 2 * omega / c_hat * Complex.Conjugate(x_N) * x_mN / (x_N2 - x_mN2);
            Complex alpha = omega / c_hat * (x_N2 + x_mN2) / (x_N2 - x_mN2) - (1 + double.Parse(D.Value) * main * main) / c_hat * Complex.ImaginaryOne;
            string serialized = "";
            serialized += Bifurcation.FilterFormulas.Serialize(main, main, Workaround_ToParserNiceString(alpha / 2));
            serialized += Bifurcation.FilterFormulas.Serialize(-main, -main, Workaround_ToParserNiceString(-Complex.Conjugate(alpha / 2)));
            serialized += Bifurcation.FilterFormulas.Serialize(main, -main, Workaround_ToParserNiceString(-Complex.Conjugate(beta / 2)));
            serialized += Bifurcation.FilterFormulas.Serialize(-main, main, Workaround_ToParserNiceString(beta / 2));

            bool useN = x_N.Magnitude > x_mN.Magnitude;
            int column = useN ? main : -main;
            Complex x_main = useN ? x_N : x_mN;

            for (int n = 2; n <= count; n++)
            {
                int m = step * n;

                Complex g = (1 + double.Parse(D.Value) * m * m + Complex.ImaginaryOne * omega) / (Complex.ImaginaryOne * c_hat);
                Complex g_m = new Complex(a_n(n) + d_n(n), c_n(n) - b_n(n)) * 0.5 * g / x_main;
                Complex g_mm = new Complex(a_n(n) - d_n(n), c_n(n) + b_n(n)) * 0.5 * g / x_main;

                string gamma_m = Workaround_ToParserNiceString(g_m);
                string gamma_mm = Workaround_ToParserNiceString(g_mm);
                if (g_m != 0)
                    serialized += Bifurcation.FilterFormulas.Serialize(m, column, gamma_m);
                if (g_mm != 0)
                    serialized += Bifurcation.FilterFormulas.Serialize(-m, column, gamma_mm);
            }

            FilterFormulas = serialized;
            IsFilterGrid = false;

            string expected = "chi";
            string expPn = $"{expAmpl * (x_N.Real + x_mN.Real)} cos ({step}x)";
            string expQn = $"0 - {expAmpl * (x_N.Real - x_mN.Real)} sin ({step}x)";
            for (int n = 2; n <= count; n++)
            {
                int m = step * n;
                double a = expAmpl * a_n(n);
                double b = expAmpl * b_n(n);
                double c = expAmpl * c_n(n);
                double d = expAmpl * d_n(n);

                expPn += " ";
                if (a > 0)
                    expPn += "+ ";
                if (a != 0)
                    expPn += $"{a} cos({m}x)";
                if (b > 0)
                    expPn += " + ";
                if (b != 0)
                    expPn += $"{b} sin({m}x)";

                expQn += " ";
                if (c > 0)
                    expQn += "+ ";
                if (c != 0)
                    expQn += $"{c} cos({m}x)";
                if (d > 0)
                    expQn += " + ";
                if (d != 0)
                    expQn += $"{d} sin({m}x)";

            }
            expected += "+ (" + expPn + $") * cos ({omega}t) + (" + expQn + $") * sin ({omega}t)";
            v.Value = expected;
        }

        private void InitTuring(double K_hat, Func<int, double> a_n, Func<int, double> b_n, Complex beta, int count = 10, int step = 1, double expAmpl = 0.125)
        {
            int main = step;
            K.Value = K_hat.ToString();
            double c_hat = K_hat * 1 * 1;
            int s = 1;
            Complex x_N = new Complex(a_n(s), -b_n(s));
            Complex x_mN = new Complex(a_n(s), b_n(s));
            Complex alpha = x_mN / x_N * Complex.Conjugate(beta) - (1 + double.Parse(D.Value) * main * main) / c_hat * Complex.ImaginaryOne;
            string serialized = "";
            serialized += Bifurcation.FilterFormulas.Serialize(main, main, Workaround_ToParserNiceString(alpha / 2));
            serialized += Bifurcation.FilterFormulas.Serialize(-main, -main, Workaround_ToParserNiceString(-Complex.Conjugate(alpha / 2)));
            serialized += Bifurcation.FilterFormulas.Serialize(main, -main, Workaround_ToParserNiceString(-Complex.Conjugate(beta / 2)));
            serialized += Bifurcation.FilterFormulas.Serialize(-main, main, Workaround_ToParserNiceString(beta / 2));

            bool useN = x_N.Magnitude > x_mN.Magnitude;
            int column = useN ? main : -main;
            Complex x_main = useN ? x_N : x_mN;

            for (int n = 2; n <= count; n++)
            {
                int m = step * n;

                Complex g = (1 + double.Parse(D.Value) * m * m) / (Complex.ImaginaryOne * c_hat);
                Complex g_m = 0.5 * g * new Complex(a_n(n), -b_n(n)) / x_main;
                Complex g_mm = 0.5 * g * new Complex(a_n(n), b_n(n)) / x_main;

                string gamma_m = Workaround_ToParserNiceString(g_m);
                string gamma_mm = Workaround_ToParserNiceString(g_mm);
                if (g_m != 0)
                    serialized += Bifurcation.FilterFormulas.Serialize(m, column, gamma_m);
                if (g_mm != 0)
                    serialized += Bifurcation.FilterFormulas.Serialize(-m, column, gamma_mm);
            }

            FilterFormulas = serialized;
            IsFilterGrid = false;

            string expected = "chi";
            string expPn = $"{expAmpl * (x_N.Real + x_mN.Real)} cos ({step}x)";
            for (int n = 2; n <= count; n++)
            {
                int m = step * n;
                double a = expAmpl * a_n(n);
                double b = expAmpl * b_n(n);

                expPn += " ";
                if (a > 0)
                    expPn += "+ ";
                if (a != 0)
                    expPn += $"{a} cos({m}x)";
                if (b > 0)
                    expPn += " + ";
                if (b != 0)
                    expPn += $"{b} sin({m}x)";

            }
            expected += $"+ {expPn}";
            v.Value = expected;
        }

        private string Workaround_ToParserNiceString(Complex c)
        {
            string res = ComplexUtils.ToNiceString(c, 5);
            if (c.Real == 0 && c.Imaginary > 0)
                res = "0 + " + res;
            if (c.Real == 0 && c.Imaginary < 0 || c.Real < 0 && c.Imaginary == 0)
                res = "0" + res;
            return res;
        }

        public void SetFilter(FilterBuilder f)
        {
            FilterBuilder = f;
            if (f is FilterGrid)
            {
                IsFilterGrid = true;
                FilterGrid = f.Filter.Matrix;
            }
            else if (f is FilterFormulas)
            {
                IsFilterGrid = false;
                FilterFormulas builder = f as FilterFormulas;
                FilterFormulas = builder.Serialize();
            }
        }

        public bool TrySet(string field, string value)
        {
            foreach (StringProperty property in Properties)
                if (property.Name == field)
                {
                    property.Value = value;
                    return true;
                }
            return false;
        }

        public void SetInput(ICollection<UIParam> parameters)
        {
            foreach (UIParam param in parameters)
                foreach (StringProperty property in Properties)
                    if (property.Name == param.Name)
                    {
                        param.Text = property.Value;
                        break;
                    }
        }

        public static SolutionInput FromFile(string path)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            dynamic inputObj = deserializer.Deserialize<ExpandoObject>(File.ReadAllText(path));

            SolutionInput res = new SolutionInput();
            object filterType = null, filter = null;
            foreach (var property in (IDictionary<string, object>)inputObj)
            {
                res.TrySet(property.Key, property.Value.ToString());
                if (property.Key == "filter_type")
                    filterType = property.Value;
                else if (property.Key == "filter")
                    filter = property.Value;
            }
            if (filterType != null && filter != null)
            {
                string type = (string)filterType;
                if (type == "grid")
                {
                    res.IsFilterGrid = true;
                    res.FilterGrid = (Complex[,])filter;
                }
                else if (type == "formulas")
                {
                    res.IsFilterGrid = false;
                    res.FilterFormulas = (string)filter;
                }
            }
            return res;
        }

        public void Save(string path)
        {
            var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();

            Dictionary<string, object> obj = new Dictionary<string, object>();
            foreach (StringProperty property in Properties)
                obj.Add(property.Name, property.Value);
            if (IsFilterGrid)
            {
                obj.Add("filter_type", "grid"); // TODO enum
                obj.Add("filter", FilterGrid);
            }
            else
            {
                FilterFormulas builder = FilterBuilder as FilterFormulas;
                FilterFormulas = builder.Serialize();
                obj.Add("filter_type", "formulas");
                obj.Add("filter", FilterFormulas);
            }

            using (StreamWriter writer = File.CreateText(path))
            {
                serializer.Serialize(writer, obj);
            }
        }
    }
}
