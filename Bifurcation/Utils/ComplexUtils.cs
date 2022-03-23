using System;
using System.Numerics;

namespace Bifurcation
{
    public static class ComplexUtils
    {
        public static Complex GetConjugate(Complex z)
        {
            return new Complex(z.Real, -z.Imaginary);
        }
        public static Complex GetQ(Complex z)
        {
            return 1 + z;
        }
        public static Complex GetConjugateQ(Complex z)
        {
            return 1 + GetConjugate(z);
        }

        public static string ToNiceString(Complex z, int maxSignificantDigits = 15)
        {
            double real = z.Real;
            double img = z.Imaginary;
            string text = "";
            if (real != 0 || img == 0)
                text += DoubleToString(real, maxSignificantDigits);
            if (real != 0)
            {
                if (img > 0)
                    text += " + " + DoubleToString(img, maxSignificantDigits) + "i";
                else if (img < 0)
                    text += " - " + DoubleToString(-img, maxSignificantDigits) + "i";
            }
            else if (img != 0)
            {
                text += DoubleToString(img, maxSignificantDigits) + "i";
            }
            return text;
        }
        public static string DoubleToString(double d, int digits)
        {
            string res = RoundToSignificantDigits(d, digits).ToString("f" + digits);
            int dotIndex = res.IndexOf('.');
            if (dotIndex > 0)
            {
                int index = Math.Min(dotIndex + digits, res.Length - 1);
                while (index > dotIndex && res[index] == '0')
                    index--;
                if (index == dotIndex)
                    index--;

                return res.Substring(0, index + 1);
            }
            return res;
        }
        // https://stackoverflow.com/a/374470
        private static double RoundToSignificantDigits(double d, int digits)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);
            return scale * Math.Round(d / scale, digits);
        }
        private static double TruncateToSignificantDigits(this double d, int digits)
        {
            if (d == 0)
                return 0;

            double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1 - digits);
            return scale * Math.Truncate(d / scale);
        }

        public static Complex Parse(string number)
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
                if (!imgDefined && (s == "" || s == "+" || s == "-"))
                    s = s + "1";

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
            if (s.Length == 0)
                return index;
            char c = s[index];
            if (c == '+' || c == '-')
            {
                if (++index == s.Length)
                    return index;
                c = s[index];
            }
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
