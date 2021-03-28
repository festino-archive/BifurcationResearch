using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static Bifurcation.Utils;

namespace Bifurcation
{
    class FormulaSlot
    {
        public double Value { get; private set; }

        private readonly FilterFormulas Builder;
        private readonly List<FormulaSlot> Formulas;
        private readonly WrapPanel Holder;
        private readonly Grid Container;
        public readonly int K, N;

        public FormulaSlot(FilterFormulas builder, List<FormulaSlot> formulas, int k, int n, WrapPanel holder)
        {
            Builder = builder;
            Holder = holder;
            Formulas = formulas;
            K = k;
            N = n;

            Container = new Grid();
            Container.ColumnDefinitions.Add(ColumnAuto());
            Container.ColumnDefinitions.Add(ColumnAuto());
            Container.ColumnDefinitions.Add(ColumnAuto());
            Container.RowDefinitions.Add(RowAuto());
            Container.RowDefinitions.Add(RowAuto());
            holder.Children.Add(Container);

            Button removeButton = new Button()
            {
                Content = "✕", VerticalContentAlignment = VerticalAlignment.Stretch,
                MinWidth = 20, Height = 20,
                Margin = new Thickness(0, 0, 5, 0)
            };
            removeButton.Click += (a, b) => RemoveButton_Clicked();
            Container.Children.Add(removeButton);

            TextBlock name = new TextBlock() { Text = "ρ" };
            Grid.SetColumn(name, 1);
            Container.Children.Add(name);
            name.UpdateLayout();
            TextBlock nameIndex = new TextBlock() { Text = k.ToString() + "," + n.ToString() };
            nameIndex.FontSize = name.FontSize * 0.8;
            nameIndex.Margin = new Thickness(name.ActualWidth, name.ActualHeight - nameIndex.FontSize * 0.8, 0, 0);
            name.Text = "ρ   = ";
            Grid.SetColumn(nameIndex, 1);
            Container.Children.Add(nameIndex);

            TextBox input = new TextBox()
            {
                Text = "0", MinWidth = 30,
                Height = name.ActualHeight, VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 15, 0)
            };
            input.TextChanged += (a, b) => Input_TextChanged();
            TextBlock result = new TextBlock() { Text = "0" };
            Grid.SetColumn(input, 2);
            Grid.SetColumn(result, 2);
            Grid.SetRow(result, 1);
            Container.Children.Add(input);
            Container.Children.Add(result);
        }

        private void Input_TextChanged()
        {
            // try parse, using inner references => error if looped
            // update outer references
        }

        private void RemoveButton_Clicked()
        {
            Holder.Children.Remove(Container);
            Builder.Remove(K, N);
        }
    }
}
