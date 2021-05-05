using System.Windows.Controls;
using HeatSim;

namespace Bifurcation
{
    public class DynamicParameter
    {
        public delegate void UpdateDependencies(string[] dependencies);
        public delegate void UpdateValue(IExpression Expr);

        public event UpdateDependencies DependenciesChanged;
        public event UpdateValue ValueChanged;

        public string[] CurrentDependencies { get; private set; }
        public IExpression Expr { get; private set; }

        private readonly TextBox Source;

        public DynamicParameter(TextBox source)
        {
            Source = source;
            Source.TextChanged += (a, b) => TextChanged();
        }

        private void TextChanged()
        {
            Expr = MainParser.Parse(Source.Text);
            CurrentDependencies = MainParser.GetDependencies(Expr);
            
            DependenciesChanged?.Invoke(CurrentDependencies); // TODO send only changes
            ValueChanged?.Invoke(Expr);
        }
    }
}
