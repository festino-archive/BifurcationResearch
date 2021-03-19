using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Bifurcation.Utils;

namespace Bifurcation
{
    class Filter
    {
        public readonly Brush COLOR_VALUE = new SolidColorBrush(Color.FromRgb(235, 255, 235));
        public readonly Brush COLOR_EMPTY = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        public readonly Brush COLOR_ERROR = new SolidColorBrush(Color.FromRgb(255, 230, 230));

        private Grid matrixPanel;
        private TextBox[,] cells;
        private Complex[,] P;

        public int Size { get; private set; }

        public Filter(Grid matrixPanel)
        {
            this.matrixPanel = matrixPanel;
        }

        public Complex[,] GetFromGrid()
        {
            int fullSize = matrixPanel.ColumnDefinitions.Count - 1;
            P = new Complex[fullSize, fullSize];
            for (int i = 0; i < fullSize; i++)
                for (int j = 0; j < fullSize; j++)
                    P[i, j] = ComplexUtils.ParseComplex(cells[i, j].Text);
            return P;
        }

        public Complex[,] GetCached()
        {
            return P;
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
            this.P = P;
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
            int fullSize = 2 * Size + 1;
            P = new Complex[fullSize, fullSize];
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
                    TextBox elem = new TextBox() {
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

        public int FindDiagCriticalN(double D, double A0, double K)
        {
            bool found = false;
            int n_cap = 0;
            double speed = 0;
            for (int n = 0; n <= Size; n++)
            {
                Complex pn = P[Size + n, Size + n];
                Complex p_n = P[Size - n, Size - n];
                double re = -K * A0 * A0 * (pn.Imaginary + p_n.Imaginary) - 1 - D * n * n;
                if (re > 0)
                {
                    found = true;
                    n_cap = n;
                    double im = K * A0 * A0 * (pn.Real - p_n.Real);
                    if (im < 0)
                        n_cap = -3;
                    speed = -A0 * A0 * (pn.Imaginary + p_n.Imaginary);
                    break;
                }
            }
            if (!found)
            {
                Logger.Write("No critical n^");
                return 0;
            }
            for (int n = 0; n <= Size; n++)
            {
                if (n == n_cap || n == -n_cap)
                    continue;
                Complex p_n = P[Size - n, Size - n];
                Complex pn = P[Size + n, Size + n];
                double re = -K * A0 * A0 * (pn.Imaginary + p_n.Imaginary) - 1 - D * n * n;
                if (re > 0)
                {
                    Logger.Write($"Second \"critical\": {n_cap} and {n}");
                    return 0;
                }
            }
            string state = "non-positive";
            if (speed > 0)
                state = "positive";
            Logger.Write($"Derivative of {n_cap} is {state}: {speed}");

            return n_cap;
        }
        public double FindDiagCritical(double D, double A0, int n_cap)
        {
            Complex p_n = P[Size - n_cap, Size - n_cap];
            Complex pn = P[Size + n_cap, Size + n_cap];
            double divider = - A0 * A0 * (pn.Imaginary + p_n.Imaginary);
            double eps = 0.01;
            if (-eps < divider && divider < eps)
                if (divider < 0)
                    return double.NegativeInfinity;
                else
                    return double.PositiveInfinity;
            return (1 + D * n_cap * n_cap) / divider;
        }

        private void Cell_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            string text = textBox.Text;
            if (text == "" || text == "0")
            {
                textBox.Background = COLOR_EMPTY;
                return;
            }
            try
            {
                Complex parsed = ComplexUtils.ParseComplex(text);
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
    }
}
