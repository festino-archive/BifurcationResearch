using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
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
        public Filter Filter { get; private set; }

        public SolutionInput()
        {
            Properties.AddRange(new StringProperty[] { D, K, A_0, T, t_count, x_count, u_0, v });
        }

        public void SetFilter(Filter f)
        {
            Filter = f;
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
            foreach (var property in (IDictionary<string, object>)inputObj)
                res.TrySet(property.Key, property.Value.ToString());
            return res;
        }

        public void Save(string path)
        {
            var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .Build();
            
            using (StreamWriter writer = File.CreateText(path))
            {
                serializer.Serialize(writer, this);
            }
        }
    }
}
