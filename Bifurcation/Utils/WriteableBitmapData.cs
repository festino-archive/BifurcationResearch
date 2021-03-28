using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Bifurcation
{
    public class WriteableBitmapData
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Int32Rect Dimentions { get; private set; }
        public int NStride { get; private set; }
        public byte[] Pixels { get; private set; }

        public WriteableBitmapData(int width, int height, int nStride, byte[] imageArr)
        {
            Width = width;
            Height = height;
            NStride = nStride;
            Pixels = imageArr;
            Dimentions = new Int32Rect(0, 0, width, height);
        }
    }
}
