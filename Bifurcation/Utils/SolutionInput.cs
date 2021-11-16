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
        public StringProperty K { get; private set; } = new StringProperty("K", "3.5");
        public StringProperty A_0 { get; private set; } = new StringProperty("A_0", "1");
        public StringProperty T { get; private set; } = new StringProperty("T", "120");
        public StringProperty t_count { get; private set; } = new StringProperty("t_count", "5000");
        public StringProperty x_count { get; private set; } = new StringProperty("x_count", "256");
        public StringProperty u_0 { get; private set; } = new StringProperty("u_0", "chi + 0.1 cos 5x");
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

            Complex[,] P = new Complex[11, 11];
            P[0, 0] = new Complex(0.2, -0.3);
            P[10, 10] = new Complex(0.6, 0.3);
            P[2, 2] = new Complex(0.4, -0.159);
            P[8, 8] = new Complex(-0.4, -0.159);
            FilterGrid = P;
            IsFilterGrid = true;
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
