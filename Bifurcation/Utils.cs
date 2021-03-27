using System.Windows;
using System.Windows.Controls;

namespace Bifurcation
{
    public static class Utils
    {
        public static ColumnDefinition ColumnStarDefinition(double value)
        {
            return new ColumnDefinition() { Width = new GridLength(value, GridUnitType.Star) };
        }
        public static RowDefinition RowStarDefinition(double value)
        {
            return new RowDefinition() { Height = new GridLength(value, GridUnitType.Star) };
        }
        public static ColumnDefinition ColumnPixelDefinition(double value)
        {
            return new ColumnDefinition() { Width = new GridLength(value, GridUnitType.Pixel) };
        }
        public static RowDefinition RowPixelDefinition(double value)
        {
            return new RowDefinition() { Height = new GridLength(value, GridUnitType.Pixel) };
        }
    }
}
