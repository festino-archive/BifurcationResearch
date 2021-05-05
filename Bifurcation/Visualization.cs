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
        private Solver solver;
        private double Min;
        private double Max;
        private double Average;
        private double Rescale;

        private int Width;
        private int Height;

        public Visualization(Solver solver, int width, int height)
        {
            this.solver = solver;
            Width = width;
            Height = height;
        }

        public WriteableBitmapData Draw(AsyncArg asyncArg)
        {
            solver.Solve(asyncArg);
            if (asyncArg.Token.IsCancellationRequested)
                return null;

            int maxT = solver.Solution.GetLength(0);
            int maxX = solver.Solution.GetLength(1);
            int nStride = CalcStride(Width);

            byte[] ImageArr = new byte[Height * nStride];
            //double avg = solver.Chi;
            Min = double.MaxValue;
            Max = double.MinValue;
            for (int j = 0; j < maxX; j++)
                for (int k = 0; k < maxT; k += maxT - 1)
                {
                    double value = solver.Solution[k, j];
                    if (Min > value)
                        Min = value;
                    if (Max < value)
                        Max = value;
                }
            Average = (Min + Max) / 2;
            double ampl = (Max - Min) / 2;
            Rescale = MAX_RESCALE;
            if (ampl > MIN_AMPL)
                Rescale = 1 / ampl;

            for (int Y = 0; Y < Height; Y++)
            {
                if (asyncArg.Token.IsCancellationRequested)
                    return null;
                for (int X = 0; X < Width; X++)
                {
                    int t = (int)(X / (float)Width * maxT);
                    int x = (int)(Y / (float)Height * maxX);
                    double value = solver.Solution[t, x];
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
                asyncArg.DrawProgress?.Report((Y + 1) / Height);
            }
            return new WriteableBitmapData(Width, Height, nStride, ImageArr);
        }

        public WriteableBitmapData DrawProfile(int width, int height)
        {
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

            int k = solver.TSize - 1;
            double pmin = double.MaxValue, pmax = double.MinValue;
            for (int j = 0; j < solver.XSize; j++)
            {
                double v = solver.Solution[k, j];
                if (pmin > v)
                    pmin = v;
                if (pmax < v)
                    pmax = v;
            }
            double average = (pmax + pmin) / 2;
            double ampl = (pmax - pmin) / 2;
            double rescale = MAX_RESCALE;
            if (ampl > MIN_AMPL)
                rescale = 1 / ampl;

            color = 255;
            int uPrev = 0;
            for (int x = 0; x < height; x++)
            {
                int j = x * solver.XSize / height;
                double value = solver.Solution[k, j];
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
            int maxT = solver.TSize;
            int maxX = solver.XSize;

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
                    value = solver.Solution[k, j];
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
    }
}
