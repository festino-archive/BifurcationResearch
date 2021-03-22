using System;
using System.Globalization;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HeatSim;
using static Bifurcation.Utils;

namespace Bifurcation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly double PARAMNAME_WIDTH = 40;
        private TextBox input_D, input_A0, input_K, input_T, input_M, input_N, input_u0;
        private Filter filter;
        private Solver curSolver;
        private Visualization vis;
        private TextBlock[] Tvalues;

        private CancellationTokenSource solveCancellation = null;

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("EN-US");
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RenderOptions.SetBitmapScalingMode(scopeImage, BitmapScalingMode.NearestNeighbor);

            Logger.Destination = log;
            input_D = AddParam("D", "0.01");
            input_A0 = AddParam("A0", "1");
            input_K = AddParam("K", "3.5");
            input_T = AddParam("T", "120");
            input_M = AddParam("time", "5000");
            input_N = AddParam("spatial", "256");
            input_u0 = AddParam("u0", "chi + 0.1 cos 5x");

            Complex[,] P = new Complex[11, 11];
            P[0, 0] = new Complex(0.2, -0.3);
            P[10, 10] = new Complex(0.6, 0.3);
            P[2, 2] = new Complex(0.4, -0.159);
            P[8, 8] = new Complex(-0.4, -0.159);
            filter = new Filter(matrixPanel);
            filter.Set(P);
            textBox_filterSize.Text = filter.Size.ToString();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateVisSizes();
        }

        private void UpdateVisSizes()
        {
            if (graphImage?.Source == null || profileImage?.Source == null)
                return;
            double margin = graphImage.Margin.Right + profileImage.Margin.Left;
            double xMargin = 5;
            double labelsWidth = Label_x2pi.ActualWidth * 2 + xMargin * 2;
            double labelsHeight = Label_t0.ActualHeight + 5;

            double width = visContainer.ActualWidth - margin - labelsWidth; // max sum
            double height = visContainer.ActualHeight - labelsHeight * 2; // max
            double h = graphImage.Source.Height;
            double w1 = graphImage.Source.Width;
            double w2 = profileImage.Source.Width * h / profileImage.Source.Height;
            double w = w1 + w2;

            double actualRatio = width / height;
            double ratio = w / h;
            double wRes, hRes;
            if (ratio < actualRatio)
            {
                hRes = height;
                wRes = height * ratio;
            }
            else
            {
                wRes = width;
                hRes = width / ratio;
            }

            double wRes1 = wRes * w1 / w;
            double wRes2 = wRes * w2 / w;
            graphImage.Height = profileImage.Height = hRes;
            graphImage.Width = wRes1;
            profileImage.Width = wRes2;

            // to UpdateLabels
            double T = curSolver.T;
            string textT;
            if (Math.Abs(T - (int)T) < 0.01)
                textT = ((int)T).ToString();
            else
                textT = T.ToString("f2");
            Label_tT.Text = textT;
            Label_tT.UpdateLayout();

            double left = graphImage.Margin.Left;
            double leftX = left - xMargin;
            double leftX0 = leftX - Label_x0.ActualWidth;
            double leftX2pi = leftX - Label_x2pi.ActualWidth;
            Label_x0.Margin = new Thickness(leftX0, -hRes, 0, 0);
            Label_x2pi.Margin = new Thickness(leftX2pi, hRes, 0, 0);
            double right = graphImage.Margin.Left + margin * 2 + wRes;
            double rightX = right + xMargin;
            Label_px0.Margin = new Thickness(rightX, -hRes, 0, 0);
            Label_px2pi.Margin = new Thickness(rightX, hRes, 0, 0);

            double bottomY = graphImage.Margin.Top + hRes + labelsHeight;
            Label_t0.Margin = new Thickness(left - Label_t0.ActualWidth / 2, bottomY, 0, 0);
            Label_tT.Margin = new Thickness(left + wRes1 - Label_tT.ActualWidth / 2, bottomY, 0, 0);
            // Tvalues margins, not creation
        }

        private void UpdateLabels()
        {

        }

        private void graphImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (curSolver == null)
                return;
            Point point = e.GetPosition(graphImage);
            int k = (int)(point.X / graphImage.ActualWidth * curSolver.Solution.GetLength(0));
            int j = (int)(point.Y / graphImage.ActualHeight * curSolver.Solution.GetLength(1));
            k = Math.Clamp(k, 0, curSolver.Solution.GetLength(0) - 1);
            j = Math.Clamp(j, 0, curSolver.Solution.GetLength(1) - 1);
            double u = curSolver.Solution[k, j];
            string val;
            if (u < 10000)
                val = u.ToString("f4");
            else
                val = u.ToString("g4");
            double t = curSolver.GetT(k);
            double x = curSolver.GetX(j);
            textBlock_u.Text = "u = " + val + $"   (t≈{t:f2}, x≈{x:f2})";
            if (e.LeftButton == MouseButtonState.Pressed)
                DrawScope(e.GetPosition(this), point);
            else
                HideScope();
        }

        private void graphImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DrawScope(e.GetPosition(this), e.GetPosition(graphImage));
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            HideScope();
        }

        private void DrawScope(Point windowPoint, Point picturePoint)
        {
            int SCOPE_RADIUS = 10;
            int size = 300;
            int maxT = curSolver.TSize;
            int maxX = curSolver.XSize;
            double widthRatio = maxT / graphImage.ActualWidth;
            double heightRatio = maxX / graphImage.ActualHeight;
            int kRadius = (int)(SCOPE_RADIUS * widthRatio);
            int jRadius = (int)(SCOPE_RADIUS * heightRatio);
            int kCursor = (int)(picturePoint.X * widthRatio);
            int jCursor = (int)(picturePoint.Y * heightRatio);
            WriteableBitmapData image = vis.DrawScope(kCursor, jCursor, kRadius, jRadius);
            WriteableBitmap scopeBmp = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null);
            scopeBmp.WritePixels(image.Dimentions, image.Pixels, image.NStride, 0, 0);
            scopeImage.Source = scopeBmp;
            scopeImage.Width = size;
            scopeImage.Height = size;

            scopeCanvas.Margin = new Thickness(windowPoint.X - size / 2, windowPoint.Y - size - 5, 0, 0);
            scopeCanvas.Width = size;
            scopeCanvas.Height = size;
            if (scopeCanvas.Visibility != Visibility.Visible)
                scopeCanvas.Visibility = Visibility.Visible;
        }
        private void HideScope()
        {
            if (scopeCanvas.Visibility != Visibility.Hidden)
                scopeCanvas.Visibility = Visibility.Hidden;
        }

        private TextBox AddParam(string name, string defaultValue)
        {
            Grid panel = new Grid();
            panel.Margin = new Thickness(0, 5, 0, 5);
            panel.ColumnDefinitions.Add(ColumnStarDefinition(1));
            panel.ColumnDefinitions.Add(ColumnStarDefinition(1));
            TextBlock label = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Text = name + " = "
            };
            TextBox input = new TextBox
            {
                MinWidth = 40,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = defaultValue
            };
            Grid.SetColumn(input, 1);

            panel.Children.Add(label);
            panel.Children.Add(input);
            paramPanel.Children.Add(panel);
            return input;
        }

        private async void drawButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (solveCancellation != null)
            {
                SwitchDrawButton(false);
                return;
            }

            string errorElem = "";
            double chi = 0;
            Solver newSolver;
            try
            {
                errorElem = "D";
                double D = double.Parse(input_D.Text);
                errorElem = "A0";
                Complex A0 = ComplexUtils.Parse(input_A0.Text);
                errorElem = "K";
                double K = double.Parse(input_K.Text);
                errorElem = "T";
                double T = double.Parse(input_T.Text);
                errorElem = "M";
                int M = int.Parse(input_M.Text);
                errorElem = "N";
                int N = int.Parse(input_N.Text);

                errorElem = "P";
                Complex[,] P = filter.GetFromGrid();
                int n = filter.Size;

                errorElem = "u0";
                ExprParser parser = new ExprParser();
                parser.AddAliases(MathAliases.GetDefaultFunctions());
                parser.AddAlias(MathAliases.ConvertName("chi"), 0);
                parser.AddAlias(MathAliases.ConvertName("x"), 0);
                IExpression expr = parser.Parse(input_u0.Text);
                expr = ExprSimplifier.Simplify(expr);
                textBlock_u0.Text = "u0 = " + expr.AsString();

                chi = Solver.GetChi(K, P[n, n], A0);
                expr = ExprSimplifier.Substitute(expr, MathAliases.ConvertName("chi"), new ExprConst(chi.ToString("f15")));
                double[] u0 = new double[N];
                for (int j = 0; j < u0.Length; j++)
                {
                    double x = 2 * Math.PI * j / u0.Length;
                    //u0[j] = Solver.GetChi(K, P[n, n]) + 0.1 * Math.Cos(5 * x);
                    IExpression substituted = ExprSimplifier.Substitute(expr, "x", new ExprConst(x.ToString("f15")));
                    substituted = ExprSimplifier.Simplify(substituted);
                    errorElem = "u0 finale";
                    u0[j] = ExprDoubleSimplifier.CalcConstExpr(substituted);
                    errorElem = "u0";
                }

                errorElem = "solver";
                newSolver = new Solver(P, T, N, M);
                newSolver.SetParams(A0, K, u0, D);
            }
            catch(Exception ex)
            {
                Logger.Write(errorElem + ": " + ex.Message);
                return;
            }

            calcBar.Value = drawBar.Value = 0;
            ShowProgress();
            SwitchDrawButton(true);

            vis = new Visualization(newSolver, 700, 200);
            var calcProgress = new Progress<double>(value => calcBar.Value = value);
            var drawProgress = new Progress<double>(value => drawBar.Value = value);
            AsyncArg asyncArg = new AsyncArg(calcProgress, drawProgress, solveCancellation.Token);

            WriteableBitmapData image = await Task.Run(() => vis.Draw(asyncArg));
            if (asyncArg.token.IsCancellationRequested)
                return;
            WriteableBitmap bmp = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null);
            bmp.WritePixels(image.Dimentions, image.Pixels, image.NStride, 0, 0);
            graphImage.Source = bmp;

            image = vis.DrawProfile(200, 500);
            WriteableBitmap profileBmp = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null);
            profileBmp.WritePixels(image.Dimentions, image.Pixels, image.NStride, 0, 0);
            profileImage.Source = profileBmp;

            curSolver = newSolver;

            var eigen = curSolver.GetEigenValues();
            Logger.Write("eigenvalues:");
            Logger.Write(eigen.Item1);
            Logger.Write("eigenvectors: (columns)");
            Logger.Write(eigen.Item2);

            textBlock_Khi.Text = "𝜒 = " + newSolver.Chi.ToString("f4");
            if (filter.IsDiagonal())
            {
                int n_cap = filter.FindDiagCriticalN(newSolver.D, 1, newSolver.K);
                textBlock_K_cap.Text = $"K^({n_cap}) = " + filter.FindDiagCritical(newSolver.D, 1, n_cap);
                textBlock_n_cap.Text = "n^ = " + n_cap;
                textBlock_n_cap_vec.Text = "";
            }
            else
            {
                textBlock_K_cap.Text = "Filter is not diagonal";
                int count = 0;
                int[] n_cap = new int[eigen.Item1.Length];
                bool multi = false;
                Complex value = 0;
                for (int n = 0; n < eigen.Item1.Length; n++)
                {
                    Complex v = eigen.Item1[n];
                    if (v.Real > -0.001)
                    {
                        if (Math.Abs(value.Imaginary - v.Imaginary) >= 0.001)
                            multi = true;
                        n_cap[count] = n;
                        count++;
                        value = v;
                    }
                }
                if (count == 0)
                    textBlock_n_cap.Text = "No n^";
                else if (multi)
                    textBlock_n_cap.Text = "Multi n^";
                else
                {
                    string text = "λn^ = " + value.ToString("f3") + " (multiplicity=" + count;
                    if (count == 1)
                        text += ", derivative=" + curSolver.GetDerivative(n_cap[0]) + ")";
                    else
                    {
                        text += ")\nderivatives:[";
                        for (int i = 0; i < count; i++)
                        {
                            if (i > 0)
                                text += ", ";
                            text += curSolver.GetDerivative(n_cap[i]);
                        }
                        text += "]";
                    }
                    textBlock_n_cap.Text = text;
                    string vec = "";
                    for (int n = 0; n < count; n++)
                    {
                        if (n > 0)
                            vec += "\n";
                        for (int i = 0; i < eigen.Item1.Length; i++)
                        {
                            if (i > 0)
                                vec += ", ";
                            vec += eigen.Item2[i, n_cap[n]].ToString("f2");
                        }
                        textBlock_n_cap_vec.Text = "e = " + vec;
                    }
                }
            }

            UpdateVisSizes();
            visContainer.Visibility = Visibility.Visible;
            SwitchDrawButton(false);
        }

        private void ShowProgress() {
            calcBar.Visibility = drawBar.Visibility = Visibility.Visible;
        }
        private void HideProgress() {
            calcBar.Visibility = drawBar.Visibility = Visibility.Hidden;
        }

        private void SwitchDrawButton(bool toWork)
        {
            if (toWork)
            {
                solveCancellation = new CancellationTokenSource();
                drawButton.Content = "Stop";
            }
            else
            {
                solveCancellation.Cancel();
                solveCancellation = null;
                drawButton.Content = "Draw";
                HideProgress();
            }
        }

        private void Psize_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            int res;
            if (!int.TryParse(textBox.Text, out res))
                return;
            if (res == filter.Size || res > 20)
                return;
            filter.Update(res);
        }

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            Logger.Clear();
        }
    }
}
