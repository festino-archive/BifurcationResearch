using System.Windows;
using System.Windows.Controls;
using static Bifurcation.GridUtils;

namespace Bifurcation
{
    public class UIParam
    {
        public string Name { get; private set; }
        public Grid Panel { get; private set; }
        public TextBox Input { get; private set; }
        public string Text { get => Input.Text; set => Input.Text = value; }

        public UIParam(string name, string labeltext, string defaultText)
        {
            Name = name;
            Panel = new Grid();
            Panel.Margin = new Thickness(0, 5, 0, 5);
            Panel.ColumnDefinitions.Add(ColumnStarDefinition(1));
            Panel.ColumnDefinitions.Add(ColumnStarDefinition(1));
            TextBlock label = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = labeltext + " = "
            };
            Input = new TextBox
            {
                MinWidth = 40,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = defaultText
            };
            Grid.SetColumn(Input, 1);

            Panel.Children.Add(label);
            Panel.Children.Add(Input);
        }
    }
}
