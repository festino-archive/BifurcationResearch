using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Text;
using System.Windows.Controls;

namespace Bifurcation
{
    internal static class Logger
    {
        public static ListBox Destination {
            get => dest;
            set
            {
                if (dest != null)
                    dest.ItemsSource = null;
                dest = value;
                dest.ItemsSource = list;
            }
        }
        private static ListBox dest = null;
        private static ObservableCollection<string> list = new ObservableCollection<string>();

        public static void Write(string s)
        {
            //dest.Dispatcher.BeginInvoke(list.Add, new string[] { s });
            dest.Dispatcher.Invoke(() => list.Add(s));
        }

        public static void Write(Array arr)
        {
            foreach (object ob in arr)
                Write(ob.ToString());
        }

        public static void Write(Complex[,] arr)
        {
            for (int i = 0; i < arr.GetLength(0); i++)
            {
                string line = "";
                if (i == 0) line += "Г ";
                else if (i == arr.GetLength(0) - 1) line += "L ";
                else line += "| ";
                for (int j = 0; j < arr.GetLength(1); j++)
                {
                    if (j > 0)
                        line += ", ";
                    line += "(" + arr[i, j].Real.ToString("f3") + "; " + arr[i, j].Imaginary.ToString("f3") + ")";
                }
                Write(line);
            }
        }

        public static void Clear()
        {
            dest.Dispatcher.Invoke(() => list.Clear());
        }
    }
}
