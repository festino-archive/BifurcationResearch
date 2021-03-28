
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using static Bifurcation.Utils;

namespace Bifurcation
{
    class FilterFormulas : FilterBuilder
    {
        public Filter Filter
        {
            get
            {
                int maxIndex = 0;
                foreach (FormulaSlot slot in Formulas)
                {
                    int index = Math.Abs(slot.K);
                    if (maxIndex < index)
                        maxIndex = index;
                    index = Math.Abs(slot.N);
                    if (maxIndex < index)
                        maxIndex = index;
                }
                int fullSize = 2 * maxIndex + 1;
                Complex[,] P = new Complex[fullSize, fullSize];
                foreach (FormulaSlot slot in Formulas)
                    P[maxIndex + slot.K, maxIndex + slot.N] = slot.Value;
                return new Filter(P);
            }
        }

        private readonly List<FormulaSlot> Formulas = new List<FormulaSlot>();
        private WrapPanel elemPanel;
        private StackPanel AddPanel;
        private Button AddButton;
        private TextBox IndexK;
        private TextBox IndexN;

        public FilterFormulas(Grid filterPanel)
        {
            filterPanel.Children.Clear();
            filterPanel.RowDefinitions.Clear();
            filterPanel.RowDefinitions.Add(RowPixelDefinition(20));
            filterPanel.RowDefinitions.Add(RowStarDefinition(1));
            filterPanel.ColumnDefinitions.Clear();
            filterPanel.ColumnDefinitions.Add(ColumnStarDefinition(1));

            TextBlock infoBlock = new TextBlock();
            infoBlock.Text = "ρ(k, n),   -1-Dn^2 = d,   iK|Ao|^2 = I";
            filterPanel.Children.Add(infoBlock);

            elemPanel = new WrapPanel();
            elemPanel.Orientation = Orientation.Vertical;
            Grid.SetRow(elemPanel, 1);
            filterPanel.Children.Add(elemPanel);

            Formulas.Add(new FormulaSlot(this, Formulas, 1, 1, elemPanel));

            AddButton = new Button()
            {
                Content = "＋", VerticalContentAlignment = VerticalAlignment.Stretch,
                MinWidth = 20, Height = 20,
                Margin = new Thickness(0, 0, 5, 0) // TODO use styles
            };
            AddButton.Click += (a, b) => AddButton_Clicked();
            TextBlock rhoText = new TextBlock() { Text = "ρ" };
            rhoText.Margin = new Thickness(0, 0, 3, 0);
            Thickness topMargin = new Thickness(0, 10, 0, 0);
            IndexK = new TextBox() { Text = "0", Margin = topMargin, MinWidth = 15 };
            IndexN = new TextBox() { Text = "0", Margin = topMargin, MinWidth = 15 };
            IndexK.TextChanged += (a, b) => CheckIndices();
            IndexN.TextChanged += (a, b) => CheckIndices();
            AddPanel = new StackPanel() { Orientation = Orientation.Horizontal };
            AddPanel.Children.Add(AddButton);
            AddPanel.Children.Add(rhoText);
            AddPanel.Children.Add(IndexK);
            AddPanel.Children.Add(IndexN);
            elemPanel.Children.Add(AddPanel);
        }

        private void AddButton_Clicked()
        {
            int K = int.Parse(IndexK.Text);
            int N = int.Parse(IndexN.Text);
            elemPanel.Children.Remove(AddPanel);
            Formulas.Add(new FormulaSlot(this, Formulas, K, N, elemPanel));
            IndexK.Text = "0";
            IndexN.Text = "0";
            CheckIndices();
            elemPanel.Children.Add(AddPanel);
        }

        private void CheckIndices()
        {
            // check both indices and check if Filter already contains it
            int K, N;
            if (!int.TryParse(IndexK.Text, out K)
                    || !int.TryParse(IndexN.Text, out N))
            {
                AddButton.IsEnabled = false;
                return;
            }
            foreach (FormulaSlot slot in Formulas)
                if (slot.K == K && slot.N == N)
                {
                    AddButton.IsEnabled = false;
                    return;
                }
            AddButton.IsEnabled = true;
        }

        public void Remove(int K, int N)
        {
            for (int i = 0; i < Formulas.Count; i++)
            {
                FormulaSlot slot = Formulas[i];
                if (slot.K == K && slot.N == N)
                {
                    Formulas.RemoveAt(i);
                    CheckIndices();
                    return;
                }
            }
        }
    }
}
