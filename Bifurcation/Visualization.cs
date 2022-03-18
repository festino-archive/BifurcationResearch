using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bifurcation
{
    class Visualization
    {
        private static readonly double MAX_RESCALE = 10_000_000_000, MIN_AMPL = 1 / MAX_RESCALE;
        private double Min;
        private double Max;
        private double Average;
        private double Rescale;

        private int Width;
        private int Height;
        double[,] solution;

        public Visualization(double[,] solution, int width, int height)
        {
            Width = width;
            Height = height;
            this.solution = solution;
        }

        public WriteableBitmapData Draw()
        {
            int maxT = solution.GetLength(0);
            int maxX = solution.GetLength(1);
            int nStride = CalcStride(Width);

            byte[] ImageArr = new byte[Height * nStride];
            //double avg = solver.Chi;
            Min = double.MaxValue;
            Max = double.MinValue;
            for (int j = 0; j < maxX; j++)
            {
                Bounds bounds = GetMinMaxForX(j);
                if (Min > bounds.Min)
                    Min = bounds.Min;
                if (Max < bounds.Max)
                    Max = bounds.Max;
            }

            Average = (Min + Max) / 2;
            double ampl = (Max - Min) / 2;
            Rescale = MAX_RESCALE;
            if (ampl > MIN_AMPL)
                Rescale = 1 / ampl;

            for (int Y = 0; Y < Height; Y++)
            {
                for (int X = 0; X < Width; X++)
                {
                    int t = (int)(X / (float)Width * maxT);
                    int x = (int)(Y / (float)Height * maxX);
                    double value = solution[t, x];
                    int index = (Y * Width + X) * 4;
                    if (double.IsNaN(value))
                    {
                        // BGRA
                        ImageArr[index + 2] = 0xFF;
                    }
                    else
                    {
                        value -= Average;
                        byte color = (byte)(128 + 127 * Math.Clamp(value * Rescale, -1, 1));
                        ImageArr[index + 0] = color;
                        ImageArr[index + 1] = color;
                        ImageArr[index + 2] = color;
                    }
                }
            }
            return new WriteableBitmapData(Width, Height, nStride, ImageArr);
        }

        public WriteableBitmapData DrawProfile(int width, int height)
        {
            int TSize = solution.GetLength(0);
            int XSize = solution.GetLength(1);
            int nStride = CalcStride(width);
            byte[] imageArr = new byte[height * nStride];
            byte color = 128;
            for (int Y = 0; Y < height; Y++)
            {
                for (int X = 0; X < width; X++)
                {
                    int index = (Y * width + X) * 4;
                    imageArr[index + 0] = color;
                    imageArr[index + 1] = color;
                    imageArr[index + 2] = color;
                }
            }

            int k = TSize - 1;
            Bounds bounds = GetMinMaxForT(k);
            double pmin = bounds.Min, pmax = bounds.Max;
            double average = (pmax + pmin) / 2;
            double ampl = (pmax - pmin) / 2;
            double rescale = MAX_RESCALE;
            if (ampl > MIN_AMPL)
                rescale = 1 / ampl;

            color = 255;
            int uPrev = 0;
            for (int x = 0; x < height; x++)
            {
                int j = x * XSize / height;
                double value = solution[k, j];
                value = (value - average) * rescale; // [-1, 1]
                value = (value + 1) / 2; // [0, 1]
                int u = (int)Math.Floor(value * width);
                u = Math.Clamp(u, 0, width - 1);
                if (x > 0)
                {
                    int min = Math.Min(u, uPrev);
                    int max = Math.Max(u, uPrev);
                    int avg = (max + min) / 2;
                    bool invert = u < uPrev;
                    for (int du = min; du <= max; du++)
                    {
                        int index = (x * width + du) * 4;
                        if (invert ^ du < avg)
                            index = ((x - 1) * width + du) * 4;
                        imageArr[index + 0] = color;
                        imageArr[index + 1] = color;
                        imageArr[index + 2] = color;
                    }
                }
                uPrev = u;
            }
            return new WriteableBitmapData(width, height, nStride, imageArr);
        }

        public WriteableBitmapData DrawScope(int kCursor, int jCursor, int kRadius, int jRadius) // maxSizes
        {
            int maxT = solution.GetLength(0);
            int maxX = solution.GetLength(1);

            int kSize = 2 * kRadius + 1;
            int jSize = 2 * jRadius + 1;
            int kLeft = kCursor - kRadius;
            int jTop = jCursor - jRadius;
            int kMin = Math.Max(0, kLeft);
            int kMax = Math.Min(maxT, kCursor + kRadius + 1);
            int jMin = jTop;
            int jMax = jCursor + jRadius + 1;
            int nStride = CalcStride(kSize);
            byte[] imageArr = new byte[jSize * nStride];
            double value;
            for (int k = kMin; k < kMax; k++)
            {
                for (int jImg = jMin; jImg < jMax; jImg++)
                {
                    int j = jImg;
                    if (j < 0) j += maxX;
                    else if (j >= maxX) j -= maxX;
                    value = solution[k, j];
                    value -= Average;
                    byte color = (byte)(128 + 127 * Math.Clamp(value * Rescale, -1, 1));
                    int index = ((jImg - jTop) * kSize + k - kLeft) * 4;
                    imageArr[index + 0] = color;
                    imageArr[index + 1] = color;
                    imageArr[index + 2] = color;
                }
            }
            if (jMin < 0 || jMax > maxX)
            {
                int jLine = 0;
                if (jMax > maxX)
                    jLine = maxX;
                int line = (jLine - jTop) * kSize;
                for (int k = kMin; k < kMax; k++)
                {
                    int index = (line + k - kLeft) * 4;
                    byte color = imageArr[index + 0];
                    if (color > 10) color -= 10;
                    imageArr[index + 0] = color;
                    imageArr[index + 1] = color;
                    imageArr[index + 2] = color;
                }

            }
            return new WriteableBitmapData(kSize, jSize, nStride, imageArr);
        }

        private int CalcStride(int width)
        {
            return (width * PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
        }

        private class Bounds
        {
            public double Min, Max;

            public Bounds(double min, double max)
            {
                this.Min = min;
                this.Max = max;
            }
        }

        private Bounds GetMinMaxForX(int j)
        {
            double min = double.MaxValue, max = double.MinValue;
            int maxT = solution.GetLength(0);
            int maxX = solution.GetLength(1);
            if (j < 0)
                j = maxX - j;
            if (j >= maxX)
                return new Bounds(min, max);

            for (int k = 0; k < maxT; k++)
            {
                double value = solution[k, j];
                if (min > value)
                    min = value;
                if (max < value)
                    max = value;
            }
            return new Bounds(min, max);
        }
        private Bounds GetMinMaxForT(int k)
        {
            double min = double.MaxValue, max = double.MinValue;
            int maxT = solution.GetLength(0);
            int maxX = solution.GetLength(1);
            if (k < 0)
                k = maxT - k;
            if (k >= maxT)
                return new Bounds(min, max);

            for (int j = 0; j < maxX; j++)
            {
                double value = solution[k, j];
                if (min > value)
                    min = value;
                if (max < value)
                    max = value;
            }
            return new Bounds(min, max);
        }
    }
}
