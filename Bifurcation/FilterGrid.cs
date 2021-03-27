using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static Bifurcation.Utils;

namespace Bifurcation
{
    class FilterGrid : FilterBuilder
    {
        public readonly Brush COLOR_VALUE = new SolidColorBrush(Color.FromRgb(235, 255, 235));
        public readonly Brush COLOR_EMPTY = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
        public readonly Brush COLOR_ERROR = new SolidColorBrush(Color.FromRgb(255, 230, 230));

        private TextBox sizeInput;
        private Grid matrixPanel;
        private TextBox[,] cells;

        public int Size { get; private set; }
        public Filter Filter { get; private set; }

        public FilterGrid(Grid filterPanel)
        {
            filterPanel.Children.Clear();
            filterPanel.RowDefinitions.Clear();
            filterPanel.RowDefinitions.Add(RowPixelDefinition(20));
            filterPanel.RowDefinitions.Add(RowStarDefinition(1));
            filterPanel.ColumnDefinitions.Clear();
            filterPanel.ColumnDefinitions.Add(ColumnStarDefinition(1));

            matrixPanel = new Grid() { Margin = new Thickness(5, 5, 5, 0) };
            Grid.SetRow(matrixPanel, 1);
            filterPanel.Children.Add(matrixPanel);

            StackPanel sizePanel = new StackPanel() { Orientation = Orientation.Horizontal };
            TextBlock sizeText = new TextBlock() { Text = "filter size = " };
            sizeInput = new TextBox() { Width = 40 };
            sizeInput.TextChanged += Psize_TextChanged;
            sizeInput.Text = "2";
            sizePanel.Children.Add(sizeText);
            sizePanel.Children.Add(sizeInput);
            filterPanel.Children.Add(sizePanel);
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
            sizeInput.Text = ((fullSize - 1) / 2).ToString(); // cause Update()
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

        private void Psize_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            int res;
            if (!int.TryParse(textBox.Text, out res))
                return;
            if (res == Size || res > 20)
                return;
            Update(res);
        }
    }
}
