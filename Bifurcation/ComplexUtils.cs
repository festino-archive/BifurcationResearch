using System;
using System.Numerics;

namespace Bifurcation
{
    public static class ComplexUtils
    {
        public static string ToNiceString(Complex z)
        {
            double real = z.Real;
            double img = z.Imaginary;
            string text = "";
            if (real != 0 || img == 0)
                text += real.ToString();
            if (img > 0)
                text += " + " + img.ToString() + "i";
            else if (img < 0)
                text += " - " + (-img).ToString() + "i";
            return text;
        }

        public static Complex ParseComplex(string number)
        {
            number = number.Replace(" ", "");
            int index = SelectDouble(number);
            double real, img = 0, d;
            string s = number.Substring(0, index);

            bool imgDefined = index < number.Length && number[index] == 'i';
            if (s == "")
                if (imgDefined)
                    s = "1";
                else
                    s = "0";

            if (!double.TryParse(s, out real))
                throw new Exception("\"" + s + "\" (\"" + number + "\")");
            if (imgDefined)
            {
                index++;
                img = real;
                real = 0;
            }
            if (index < number.Length)
            {
                if (imgDefined ^ number[^1] != 'i')
                    throw new Exception("imaginary error (\"" + number + "\")");
                int lastIndex = number.Length;
                if (!imgDefined)
                    lastIndex--;
                s = number.Substring(index, lastIndex - index);
                if (!imgDefined && s == "")
                    s = "1";

                if (!double.TryParse(s, out d))
                    throw new Exception("\"" + s + "\" (\"" + number + "\")");
                if (imgDefined)
                    real = d;
                else
                    img = d;
            }
            return new Complex(real, img);
        }

        private static int SelectDouble(string s)
        {
            int index = 0;
            char c = s[index];
            if (c == '+' || c == '-')
                c = s[++index];
            while ('0' <= c && c <= '9' || c == '.')
            {
                if (++index == s.Length)
                    break;
                c = s[index];
            }
            return index;
        }
    }
}
