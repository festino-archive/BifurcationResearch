using System;
using System.Windows;
using System.Windows.Controls;

namespace Bifurcation
{
    class RadioButtonGroup
    {
        public delegate void RadioButtonGroupUpdate(int index);
        public event RadioButtonGroupUpdate Changed;

        public int CheckedIndex { get; private set; }

        public RadioButtonGroup(RadioButton[] group, int checkedIndex = 0)
        {
            for (int i = 0; i < group.Length; i++)
            {
                int k = i;
                group[i].Checked += (a, b) => Checked(k);
            }
            CheckedIndex = checkedIndex;
            group[CheckedIndex].IsChecked = true;
        }

        private void Checked(int index)
        {
            if (CheckedIndex != index)
            {
                CheckedIndex = index;
                Changed?.Invoke(CheckedIndex);
            }
        }
    }
}
