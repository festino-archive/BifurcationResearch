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
        private TextBox input_D, input_K, input_T, input_M, input_N, input_u0;
        private Filter filter;
        private Solver curSolver;
        private Visualization vis;
        private WriteableBitmap bmp;

        private CancellationTokenSource solveCancellation = null;

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("EN-US");
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Destination = log;
            input_D = AddParam("D", "0.01");
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
            int pixelSize = (1 + 2 * SCOPE_RADIUS);
            int size = 300;
            scopeCanvas.Margin = new Thickness(windowPoint.X - size / 2, windowPoint.Y - size - 5, 0, 0);
            scopeCanvas.Width = size;
            scopeCanvas.Height = size;

            /*WriteableBitmap scopeBmp = new WriteableBitmap(pixelSize, pixelSize, 96, 96, PixelFormats.Bgr32, null);
            int origWidth = (int)bmp.Width;
            int origHeight = (int)bmp.Height;
            int leftPoint = Math.Clamp((int)(picturePoint.X - SCOPE_RADIUS), 0, origWidth);
            int topPoint = Math.Clamp((int)(picturePoint.Y - SCOPE_RADIUS), 0, origHeight);
            int rightPoint = Math.Clamp((int)(picturePoint.X + SCOPE_RADIUS + 1), 0, origWidth);
            int bottomPoint = Math.Clamp((int)(picturePoint.Y + SCOPE_RADIUS + 1), 0, origHeight);
            int width = rightPoint - leftPoint;
            int height = bottomPoint - topPoint;
            int leftOffset = Math.Clamp(leftPoint + SCOPE_RADIUS - (int)picturePoint.X, 0, width);
            int topOffset = Math.Clamp(topPoint + SCOPE_RADIUS - (int)picturePoint.Y, 0, height);
            Int32Rect scope = new Int32Rect(leftPoint, topPoint, width, height);
            bmp.CopyPixelsTo(scope, scopeBmp, new Int32Rect(leftOffset, topOffset, width, height));*/
            int maxT = curSolver.TSize;
            int maxX = curSolver.XSize;
            double widthRatio = maxT / graphImage.ActualWidth;
            double heightRatio = maxX / graphImage.ActualHeight;
            int kRadius = (int)(SCOPE_RADIUS * widthRatio);
            int jRadius = (int)(SCOPE_RADIUS * heightRatio);
            int kCursor = (int)(picturePoint.X * widthRatio);
            int jCursor = (int)(picturePoint.Y * heightRatio);
            byte[] imageArr = vis.DrawScope(kCursor, jCursor, kRadius, jRadius);
            int kSize = 2 * kRadius + 1;
            int jSize = 2 * jRadius + 1;
            WriteableBitmap scopeBmp = new WriteableBitmap(kSize, jSize, 96, 96, PixelFormats.Bgr32, null);
            scopeBmp.WritePixels(new Int32Rect(0, 0, kSize, jSize), imageArr, vis.ScopeStride, 0, 0);
            scopeImage.Source = scopeBmp;
            scopeImage.Width = size;
            scopeImage.Height = size;

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

                chi = Solver.GetChi(K, P[n, n]);
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
                newSolver = new Solver(P, K, u0, T, D, N, M);
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

            byte[] imageArr = await Task.Run(() => vis.Draw(asyncArg));
            if (asyncArg.token.IsCancellationRequested)
                return;
            bmp = new WriteableBitmap(vis.Width, vis.Height, 96, 96, PixelFormats.Bgr32, null);
            bmp.WritePixels(vis.ImageDimentions, imageArr, vis.NStride, 0, 0);
            graphImage.Source = bmp;

            textBlock_Khi.Text = "𝜒 = " + newSolver.Chi.ToString("f4");
            int n_cap = filter.FindDiagCriticalN(newSolver.D, 1, newSolver.K);
            textBlock_n_cap.Text = "n^ = " + n_cap;
            textBlock_K_cap.Text = $"K^({n_cap}) = " + filter.FindDiagCritical(newSolver.D, 1, n_cap);
            curSolver = newSolver;

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
