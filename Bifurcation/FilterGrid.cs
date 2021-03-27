using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Bifurcation.Utils;

namespace Bifurcation
{
    class FilterGrid
    {
        public readonly Brush COLOR_VALUE = new SolidColorBrush(Color.FromRgb(235, 255, 235));
        public readonly Brush COLOR_EMPTY = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        public readonly Brush COLOR_ERROR = new SolidColorBrush(Color.FromRgb(255, 230, 230));

        private Grid matrixPanel;
        private TextBox[,] cells;

        public int Size { get; private set; }
        public Filter Filter { get; private set; }

        public FilterGrid(Grid matrixPanel)
        {
            this.matrixPanel = matrixPanel;
        }

        public void UpdateFromGrid()
        {
            int fullSize = cells.GetLength(0);
            Complex[,] P = new Complex[fullSize, fullSize];
            for (int i = 0; i < fullSize; i++)
                for (int j = 0; j < fullSize; j++)
                    P[i, j] = ComplexUtils.Parse(cells[i, j].Text);
            Filter = new Filter(P);
        }

        public void Set(Complex[,] P)
        {
            int fullSize = P.GetLength(0);
            Update((fullSize - 1) / 2);
            foreach (UIElement elem in matrixPanel.Children)
            {
                int i = Grid.GetRow(elem) - 1;
                int j = Grid.GetColumn(elem) - 1;
                if (elem is TextBox
                        && 0 <= i && i < fullSize
                        && 0 <= j && j < fullSize)
                {
                    ((TextBox)elem).Text = ComplexUtils.ToNiceString(P[i, j]);
                }
            }
            Filter = new Filter(P);
        }

        public void Update(int size)
        {
            int oldFullSize = 2 * Size + 1;
            if (cells != null)
                for (int i = 0; i < oldFullSize; i++)
                    for (int j = 0; j < oldFullSize; j++)
                        cells[i, j].TextChanged -= Cell_TextChanged;
            matrixPanel.Children.Clear();
            matrixPanel.ColumnDefinitions.Clear();
            matrixPanel.RowDefinitions.Clear();
            Size = size;
            Filter = new Filter(Size);
            int fullSize = 2 * Size + 1;
            cells = new TextBox[fullSize, fullSize];

            matrixPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
            matrixPanel.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
            for (int i = 0; i < fullSize; i++)
            {
                matrixPanel.ColumnDefinitions.Add(ColumnStarDefinition(1));
                matrixPanel.RowDefinitions.Add(RowStarDefinition(1));
                string value = (i - Size).ToString();
                TextBlock rowNum = new TextBlock()
                {
                    Text = value,
                    Margin = new Thickness(0, 0, 5, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetRow(rowNum, i + 1);
                matrixPanel.Children.Add(rowNum);
                TextBlock columnNum = new TextBlock()
                {
                    Text = value,
                    Margin = new Thickness(0, 0, 0, 5),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(columnNum, i + 1);
                matrixPanel.Children.Add(columnNum);
            }

            for (int i = 1; i <= fullSize; i++)
                for (int j = 1; j <= fullSize; j++)
                {
                    TextBox elem = new TextBox()
                    {
                        Text = "0",
                        Background = COLOR_EMPTY
                    };
                    elem.TextChanged += Cell_TextChanged;
                    Grid.SetRow(elem, i);
                    Grid.SetColumn(elem, j);
                    matrixPanel.Children.Add(elem);
                    cells[i - 1, j - 1] = elem;
                }
        }

        private void Cell_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string text = textBox.Text;
            try
            {
                Complex parsed = ComplexUtils.Parse(text);
                int i = Grid.GetRow(textBox) - 1;
                int j = Grid.GetColumn(textBox) - 1;
                Filter[i, j] = parsed;
                if (parsed == 0)
                    textBox.Background = COLOR_EMPTY;
                else
                    textBox.Background = COLOR_VALUE;
            }
            catch
            {
                textBox.Background = COLOR_ERROR;
            }
        }

        public void UpdateEigen(TextBlock textBlock, ModelParams param)
        {
            var eigen = Filter.GetEigenValues(param);
            Logger.Write("eigenvalues:");
            Logger.Write(eigen.Item1);
            Logger.Write("eigenvectors: (columns)");
            Logger.Write(eigen.Item2);

            if (Filter.IsDiagonal)
            {
                int n_cap = Filter.FindDiagCriticalN(param.D, 1, param.K);
                textBlock.Text = $"K^({n_cap}) = " + Filter.FindDiagCritical(param.D, 1, n_cap);
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
                        text += ", derivative=" + Filter.GetDerivative(n_cap[0], param).ToString("f2") + ")";
                    else
                    {
                        text += ")\nderivatives:[";
                        for (int i = 0; i < count; i++)
                        {
                            if (i > 0)
                                text += ", ";
                            text += Filter.GetDerivative(n_cap[i], param).ToString("f2");
                        }
                        text += "]";
                    }
                    textBlock.Text = text;
                }
            }
        }
    }
}
