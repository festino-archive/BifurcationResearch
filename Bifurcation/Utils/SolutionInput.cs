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

            /*int freak = 1;
            int count = 10;
            int main = freak;
            double c = double.Parse(K.Value) * 1 * 1;
            double hat_c = (double.Parse(K.Value) - 0.1) * 1 * 1;
            Complex beta = new Complex(0.2, -0.2);
            Complex alpha = Complex.Conjugate(beta) - new Complex(0, (1 + double.Parse(D.Value) * main * main) / hat_c);
            string serialized = "";
            serialized += Bifurcation.FilterFormulas.Serialize(main, main, Workaround_ToParserNiceString(alpha / 2));
            serialized += Bifurcation.FilterFormulas.Serialize(-main, -main, Workaround_ToParserNiceString(-Complex.Conjugate(alpha / 2)));
            serialized += Bifurcation.FilterFormulas.Serialize(main, -main, Workaround_ToParserNiceString(-Complex.Conjugate(beta / 2)));
            serialized += Bifurcation.FilterFormulas.Serialize(-main, main, Workaround_ToParserNiceString(beta / 2));
            Logger.Write($"alpha_{main} = {alpha}, beta_{main} = {beta}");
            for (int i = 2; i <= count; i++)
            {
                int n = 2 * i - 1;
                int m = freak * n;
                double g = (1 + double.Parse(D.Value) * m * m) / c / n;
                if (i % 2 == 1)
                    g *= -1;

                string gamma = Workaround_ToParserNiceString(new Complex(0, g / 4));
                serialized += Bifurcation.FilterFormulas.Serialize(m, main, gamma);
                serialized += Bifurcation.FilterFormulas.Serialize(m, -main, gamma);
                serialized += Bifurcation.FilterFormulas.Serialize(-m, main, gamma);
                serialized += Bifurcation.FilterFormulas.Serialize(-m, -main, gamma);
                Logger.Write($"gamma_{m} = {g}, rho_{m},{main} = {gamma}, rho_{m},{-main} = {gamma}, " +
                    $"rho_{-m},{main} = {gamma}, rho_{-m},{-main} = {gamma}");
                Console.WriteLine($"gamma_{m} = {g}, rho_{m},{main} = {gamma}, rho_{m},{-main} = {gamma}, " +
                    $"rho_{-m},{main} = {gamma}, rho_{-m},{-main} = {gamma}");
            }

            FilterFormulas = serialized;
            IsFilterGrid = false;

            string expected = "chi";
            for (int i = 1; i <= count; i++)
            {
                int n = 2 * i - 1;
                int m = freak * n;
                double g = 0.448 / n;
                if (i % 2 == 0)
                    g *= -1;

                expected += " ";
                if (g > 0)
                    expected += "+";
                expected += $"{g} cos({m}x)";
            }
            v.Value = expected;
            */


            int freak = 1;
            int count = 10;
            int main = freak;
            double c = double.Parse(K.Value) * 1 * 1;
            double hat_c = (double.Parse(K.Value) - 0.05) * 1 * 1;
            Complex x_mN = Complex.FromPolarCoordinates(0.5, 0.0);
            double omega = 1;
            Complex beta = 2 * omega / c * x_mN / (1 - x_mN * Complex.Conjugate(x_mN));
            Complex alpha = Complex.Conjugate(beta) * x_mN + new Complex(omega / c, -(1 + double.Parse(D.Value) * main * main) / hat_c);
            string serialized = "";
            serialized += Bifurcation.FilterFormulas.Serialize(main, main, Workaround_ToParserNiceString(alpha / 2));
            serialized += Bifurcation.FilterFormulas.Serialize(-main, -main, Workaround_ToParserNiceString(-Complex.Conjugate(alpha / 2)));
            serialized += Bifurcation.FilterFormulas.Serialize(main, -main, Workaround_ToParserNiceString(-Complex.Conjugate(beta / 2)));
            serialized += Bifurcation.FilterFormulas.Serialize(-main, main, Workaround_ToParserNiceString(beta / 2));
            for (int i = 2; i <= count; i++)
            {
                int n = 2 * i - 1;
                int m = freak * n;
                Complex g = (1 + double.Parse(D.Value) * m * m + Complex.ImaginaryOne * omega) / c / n;
                if (i % 2 == 1)
                    g *= -1;

                string gamma = Workaround_ToParserNiceString(Complex.ImaginaryOne * g / 4);
                serialized += Bifurcation.FilterFormulas.Serialize(m, main, gamma);
                serialized += Bifurcation.FilterFormulas.Serialize(m, -main, gamma);
                serialized += Bifurcation.FilterFormulas.Serialize(-m, main, gamma);
                serialized += Bifurcation.FilterFormulas.Serialize(-m, -main, gamma);
                Console.WriteLine($"gamma_{m} = {g}, rho_{m},{main} = {gamma}, rho_{m},{-main} = {gamma}, " +
                    $"rho_{-m},{main} = {gamma}, rho_{-m},{-main} = {gamma}");
            }

            FilterFormulas = serialized;
            IsFilterGrid = false;

            string expected = "chi";
            for (int i = 1; i <= count; i++)
            {
                int n = 2 * i - 1;
                int m = freak * n;
                double g = 0.448 / n;
                if (i % 2 == 0)
                    g *= -1;

                expected += " ";
                if (g > 0)
                    expected += "+";
                expected += $"{g} cos({m}x)";
            }
            v.Value = expected;
        }

        private string Workaround_ToParserNiceString(Complex c)
        {
            string res = ComplexUtils.ToNiceString(c, 5);
            if (c.Real == 0 && c.Imaginary > 0)
                res = "0 + " + res;
            if (c.Real == 0 && c.Imaginary < 0)
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
