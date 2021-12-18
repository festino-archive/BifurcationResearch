using InteractiveDataDisplay.WPF;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Bifurcation
{
    /// <summary>
    /// Interaction logic for PlotWindow.xaml
    /// </summary>
    public partial class PlotWindow : Window
    {
        private Color[] colors = new Color[] {
            Color.FromArgb(0xFF, 0xAF, 0, 0),
            Color.FromArgb(0xFF, 0, 0xAF, 0),
            Color.FromArgb(0xFF, 0, 0, 0xAF),
            Color.FromArgb(0xFF, 0x5F, 0x5F, 0),
            Color.FromArgb(0xFF, 0, 0x5F, 0x5F),
            Color.FromArgb(0xFF, 0x5F, 0, 0x5F),
        };
        private int colorIndex = 0;

        public PlotWindow()
        {
            InitializeComponent();
        }

        public void DrawLinePlot(double minX, double maxX, double[] values, string name)
        {
            int pointCount = values.Length;
            double[] xs = Consecutive(pointCount, minX, (maxX - minX) / pointCount);

            colorIndex = (colorIndex + 1) % colors.Length;
            LineGraph line = AddLine(name, colors[colorIndex]);

            line.Plot(xs, values);

            myGrid.Children.Add(line);

            myChart.Title = $"Графики ({pointCount:n0} точек по Ox)";
            myChart.BottomTitle = $"Угол, радианы";
            myChart.LeftTitle = $"Фазовая модуляция";
            myChart.IsAutoFitEnabled = true;
            myChart.LegendVisibility = Visibility.Visible;
        }

        private LineGraph AddLine(string name, Color color)
        {
            LineGraph line = new LineGraph
            {
                Stroke = new SolidColorBrush(color),
                Description = name,
                StrokeThickness = 2
            };

            Button button = new Button
            {
                Content = "- " + name,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(3),
                Padding = new Thickness(3)
            };
            button.Click += (s, e) =>
            {
                RemoveLine(line);
                stackPanel_LineButtons.Children.Remove(button);
            };
            stackPanel_LineButtons.Children.Add(button);

            return line;
        }

        private void RemoveLine(LineGraph line)
        {
            myGrid.Children.Remove(line);
        }

        public void Clear()
        {
            stackPanel_LineButtons.Children.Clear();
            myGrid.Children.Clear();
            colorIndex = 0;
        }

        // https://swharden.com/CsharpDataVis/plotting/interactive-data-display.md.html
        private double[] Consecutive(int points, double offset = 0, double stepSize = 1)
        {
            // return an array of ascending numbers starting at 1
            double[] values = new double[points];
            for (int i = 0; i < points; i++)
                values[i] = i * stepSize + offset;
            return values;
        }

        private void button_ClearPlot_Click(object sender, RoutedEventArgs e)
        {
            Clear();
        }
    }
}
