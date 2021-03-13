using System.Windows;
using System.Windows.Controls;

namespace Bifurcation
{
    public static class Utils
    {
        public static ColumnDefinition ColumnStarDefinition(double value)
        {
            return new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
        }
        public static RowDefinition RowStarDefinition(double value)
        {
            return new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) };
        }
    }
}
