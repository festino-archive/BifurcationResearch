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
        private Solver solver;
        private double Average;
        private double Rescale;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public int NStride { get; private set; }
        public int ScopeStride { get; private set; }
        public Int32Rect ImageDimentions { get; private set; }

        public Visualization(Solver solver, int width, int height)
        {
            this.solver = solver;
            Width = width;
            Height = height;
            NStride = (Width * PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            ImageDimentions = new Int32Rect(0, 0, width, height);
        }

        public byte[] Draw(AsyncArg asyncArg)
        {
            solver.Solve(asyncArg);
            if (asyncArg.token.IsCancellationRequested)
                return null;

            int maxT = solver.Solution.GetLength(0);
            int maxX = solver.Solution.GetLength(1);

            byte[] ImageArr = new byte[Height * NStride];
            //double avg = solver.Chi;
            double min = double.MaxValue, max = double.MinValue;
            for (int j = 0; j < maxX; j++)
                for (int k = 0; k < maxT; k += maxT - 1)
                {
                    double value = solver.Solution[k, j];
                    if (min > value)
                        min = value;
                    if (max < value)
                        max = value;
                }
            Average = (min + max) / 2;
            double ampl = (max - min) / 2;
            Rescale = 1;
            if (ampl > 0.001)
                Rescale = 1 / ampl;

            for (int Y = 0; Y < Height; Y++)
            {
                if (asyncArg.token.IsCancellationRequested)
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
                asyncArg.drawProgress?.Report((Y + 1) / Height);
            }
            return ImageArr;
        }

        public byte[] DrawScope(int kCursor, int jCursor, int kRadius, int jRadius) // maxSizes
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
            ScopeStride = (kSize * PixelFormats.Bgra32.BitsPerPixel + 7) / 8;
            byte[] imageArr = new byte[jSize * ScopeStride];
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
            return imageArr;
        }
    }
}
