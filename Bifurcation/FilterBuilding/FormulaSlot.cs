﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using static Bifurcation.GridUtils;

namespace Bifurcation
{
    class FormulaSlot
    {
        private readonly FilterFormulas Builder;
        private readonly WrapPanel Holder;
        private readonly Grid Container;
        public readonly int K, N;
        public TextBox Input { get; }
        public TextBlock Result { get; }

        public FormulaSlot(FilterFormulas builder, int k, int n, WrapPanel holder)
        {
            Builder = builder;
            Holder = holder;
            K = k;
            N = n;

            Container = new Grid();
            Container.ColumnDefinitions.Add(ColumnAuto());
            Container.ColumnDefinitions.Add(ColumnAuto());
            Container.ColumnDefinitions.Add(ColumnAuto());
            Container.RowDefinitions.Add(RowAuto());
            Container.RowDefinitions.Add(RowAuto());
            holder.Children.Add(Container);

            Button removeButton = new Button()
            {
                Content = "✕", VerticalContentAlignment = VerticalAlignment.Stretch,
                MinWidth = 20, Height = 20,
                Margin = new Thickness(0, 0, 5, 0)
            };
            removeButton.Click += (a, b) => RemoveButton_Clicked();
            Container.Children.Add(removeButton);

            TextBlock name = new TextBlock() { Text = "ρ" };
            Grid.SetColumn(name, 1);
            Container.Children.Add(name);
            name.UpdateLayout();
            TextBlock nameIndex = new TextBlock() { Text = k.ToString() + "," + n.ToString() };
            nameIndex.FontSize = name.FontSize * 0.8;
            nameIndex.Margin = new Thickness(name.ActualWidth, name.ActualHeight - nameIndex.FontSize * 0.8, 0, 0);
            name.Text = "ρ   = ";
            Grid.SetColumn(nameIndex, 1);
            Container.Children.Add(nameIndex);

            Input = new TextBox()
            {
                Text = "0", MinWidth = 30,
                Height = name.ActualHeight, VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 15, 0)
            };
            Result = new TextBlock() { Text = "0" };
            Grid.SetColumn(Input, 2);
            Grid.SetColumn(Result, 2);
            Grid.SetRow(Result, 1);
            Container.Children.Add(Input);
            Container.Children.Add(Result);
        }

        private void RemoveButton_Clicked()
        {
            Holder.Children.Remove(Container);
            Builder.Remove(K, N);
        }

        internal void Remove()
        {
            Holder.Children.Remove(Container);
        }
    }
}
