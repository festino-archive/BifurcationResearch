using HeatSim;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Controls;

namespace Bifurcation
{
    public class DependencyNode
    {
        public delegate void UpdateDependencies(DependencyNode sender, string[] dependencies);
        public event UpdateDependencies DependenciesChanged;

        public string Name { get; }
        public List<DependencyNode> DependsOn { get; } = new List<DependencyNode>();
        public List<DependencyNode> Dependencies { get; } = new List<DependencyNode>();
        public IExpression Expr { get; private set; }
        public Complex Value { get; private set; }
        private DynamicParameter Param { get; }

        public DependencyNode(string name, TextBox input)
        {
            Name = name;
            Param = new DynamicParameter(input);
            Param.ValueChanged += (expr) => ValueChanged(expr);
            Param.DependenciesChanged += (d) => Param_DependenciesChanged(d);
        }

        private void ValueChanged(IExpression expr)
        {
            Value = MainParser.Eval(expr, DependsOn);
            Expr = expr;
        }

        private void Param_DependenciesChanged(string[] deps)
        {
            DependenciesChanged?.Invoke(this, deps);
        }
    }
}
