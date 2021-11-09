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
using static Bifurcation.GridUtils;

namespace Bifurcation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly int VISUALIZATION_WIDTH = 700, VISUALIZATION_HEIGHT = 200;

        private TextBox input_D, input_A0, input_K, input_T, input_M, input_N, input_u0, input_v;
        private RadioButtonGroup FilterModeGroup;
        private RadioButtonGroup SolutionMethodGroup;
        private FilterBuilder filterBuilder;
        private DependencySpace Dependencies;

        private Solver curSolver;
        private Visualization vis;
        private TextBlock[] Tvalues;

        private CancellationTokenSource solveCancellation = null;
        private Solver.Method method;

        public MainWindow()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("EN-US");
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Logger.Destination = log;
            RenderOptions.SetBitmapScalingMode(scopeImage, BitmapScalingMode.NearestNeighbor);

            RadioButton[] filterModeButtons = { matrixRadioButton, formulaRadioButton };
            FilterModeGroup = new RadioButtonGroup(filterModeButtons);
            FilterModeGroup.Changed += FillModeChanged;

            RadioButton[] solMethodButtons = { explicitRadioButton, implicitRadioButton };
            SolutionMethodGroup = new RadioButtonGroup(solMethodButtons);
            SolutionMethodGroup.Changed += SolutionMethodChanged;

            Dependencies = new DependencySpace();
            input_D = AddParam("D", "0.01");
            input_A0 = AddParam("A0", "1");
            input_K = AddParam("K", "3.5");
            input_T = AddParam("T", "120");
            input_M = AddParam("time", "5000");
            input_N = AddParam("spatial", "256");
            input_u0 = AddParam("u0", "chi + 0.1 cos 5x");
            AddLabel("");
            input_v = AddParam("v(x, t)", "chi + cos(0.1t) * cos 3x + sin(0.1t) * sin 3x");
            AddButton("Draw", (a, b) => DrawExpectedSolution());

            Complex[,] P = new Complex[11, 11];
            P[0, 0] = new Complex(0.2, -0.3);
            P[10, 10] = new Complex(0.6, 0.3);
            P[2, 2] = new Complex(0.4, -0.159);
            P[8, 8] = new Complex(-0.4, -0.159);
            FilterGrid filterGrid = new FilterGrid(filterPanel);
            filterGrid.Set(P);
            filterBuilder = filterGrid;
        }

        private void SolutionMethodChanged(int index)
        {
            if (index == 0)
                method = Solver.Method.EXPLICIT;
            else if (index == 1)
                method = Solver.Method.IMPLICIT_2;
        }

        private void FillModeChanged(int index)
        {
            Dependencies.RemoveFilter();
            if (index == 0)
                filterBuilder = new FilterGrid(filterPanel);
            else if (index == 1)
                filterBuilder = new FilterFormulas(filterPanel, Dependencies);
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

        private void AddLabel(string text)
        {
            TextBlock label = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = text
            };
            paramPanel.Children.Add(label);
        }

        private void AddButton(string text, RoutedEventHandler onClick)
        {
            Button button = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Content = text,
                Padding = new Thickness(5)
            };
            button.Click += onClick;
            paramPanel.Children.Add(button);
        }

        private void DrawExpectedSolution()
        {
            IExpression expr = MainParser.Parse(input_v.Text);
            string[] deps = MainParser.GetDependencies(expr);
            double T = double.Parse(input_T.Text);
            //int M = int.Parse(input_M.Text);
            //int N = int.Parse(input_N.Text);
            //double[,] sol = MainParser.EvalMatrixD(expr, Dependencies, deps, "t", T, M, "x", 2 * Math.PI, N);
            double[,] sol = MainParser.EvalMatrixD(expr, Dependencies, deps, "t", T, VISUALIZATION_HEIGHT, "x", 2 * Math.PI, VISUALIZATION_WIDTH);
            UpdateVisualization(sol);
        }

        private async void drawButton_ClickAsync(object sender, RoutedEventArgs e)
        {
            if (solveCancellation != null)
            {
                SwitchDrawButton(false);
                return;
            }

            BuildSolver();

            SwitchDrawButton(true);
            calcBar.Value = 0;
            var calcProgress = new Progress<double>(value => calcBar.Value = value);
            ShowProgress();

            Task.Run(() =>
            {
                AsyncArg asyncArg = new AsyncArg(method, calcProgress, solveCancellation.Token);
                curSolver.Solve(asyncArg);

                this.Dispatcher.Invoke(() =>
                {
                    textBlock_Khi.Text = "𝜒 = " + curSolver.Chi.ToString("f4");
                    FilterInfo.UpdateEigen(filterBuilder.Filter, textBlock_critical, curSolver.Parameters);

                    UpdateVisualization(curSolver.Solution);
                    SwitchDrawButton(false);
                });
            });
        }

        private void BuildSolver()
        {

            Filter P = filterBuilder.Filter;

            Solver newSolver;
            string errorElem = "";
            double chi = 0;
            try
            {
                int n = P.Size;
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

                chi = Solver.GetChi(K, P[n, n], A0);
                Dependencies.Set(MathAliases.ConvertName("chi"), chi);

                errorElem = "u0";
                IExpression expr = MainParser.Parse(input_u0.Text);
                textBlock_u0.Text = "u0 = " + expr.AsString();

                string[] deps = MainParser.GetDependencies(expr);
                double[] u0 = MainParser.EvalArrayD(expr, Dependencies, deps, "x", 2 * Math.PI, N);

                errorElem = "solver";
                newSolver = new Solver(P, T, N, M);
                ModelParams param = new ModelParams(A0, K, u0, D);
                newSolver.SetParams(param);
                curSolver = newSolver;
            }
            catch (Exception ex)
            {
                Logger.Write(errorElem + ": " + ex.Message);
                return;
            }
        }

        private void UpdateVisualization(double[,] solution)
        {
            vis = new Visualization(solution, VISUALIZATION_WIDTH, VISUALIZATION_HEIGHT);

            WriteableBitmapData image = vis.Draw();

            WriteableBitmap bmp = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null);
            bmp.WritePixels(image.Dimentions, image.Pixels, image.NStride, 0, 0);
            graphImage.Source = bmp;

            image = vis.DrawProfile(200, 500);
            WriteableBitmap profileBmp = new WriteableBitmap(image.Width, image.Height, 96, 96, PixelFormats.Bgr32, null);
            profileBmp.WritePixels(image.Dimentions, image.Pixels, image.NStride, 0, 0);
            profileImage.Source = profileBmp;

            UpdateVisSizes();
            visContainer.Visibility = Visibility.Visible;
        }

        private void ShowProgress() {
            calcBar.Visibility = Visibility.Visible;
        }
        private void HideProgress() {
            calcBar.Visibility = Visibility.Hidden;
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

        private void ClearLog_Click(object sender, RoutedEventArgs e)
        {
            Logger.Clear();
        }
    }
}
