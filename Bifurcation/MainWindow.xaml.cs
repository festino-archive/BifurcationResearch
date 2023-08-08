using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HeatSim;
using Microsoft.Win32;

namespace Bifurcation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static readonly int VISUALIZATION_WIDTH = 2100, VISUALIZATION_HEIGHT = 600;

        private List<UIParam> parameters = new List<UIParam>();
        private RadioButtonGroup FilterModeGroup;
        private RadioButtonGroup SolutionMethodGroup;
        private FilterBuilder filterBuilder;
        private DependencySpace Dependencies;

        private Solver curSolver;
        private Visualization vis;
        private TextBlock[] Tvalues;

        private CancellationTokenSource solveCancellation = null;
        private Solver.Method method;

        private PlotWindow plotWindow;

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
            AddParam("D", "D");
            AddParam("A_0", "A0");
            AddParam("K", "K");
            AddParam("T", "T");
            AddParam("t_count", "time");
            AddParam("x_count", "spatial");
            AddParam("u_0", "u0(x)");
            AddLabel("");
            AddParam("v", "v(x, t)");
            AddButton("Draw v(x,t)", (a, b) => DrawExpectedSolution());
            SolutionInput defaultInput = new SolutionInput();
            defaultInput.SetInput(parameters);

            if (defaultInput.IsFilterGrid)
            {
                FilterGrid filterGrid = new FilterGrid(filterPanel);
                filterGrid.Set(defaultInput.FilterGrid);
                filterBuilder = filterGrid;
            }
            else
            {
                FilterFormulas filterFormulas = new FilterFormulas(filterPanel, Dependencies);
                filterFormulas.Deserialize(defaultInput.FilterFormulas);
                filterBuilder = filterFormulas;
            }
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

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.RestoreDirectory = true;
            dialog.Filter = "yml files (*.yml)|*.yml|All files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                SolutionInput input = BuildInput();
                input.Save(dialog.FileName);
            }
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.InitialDirectory = Directory.GetCurrentDirectory();
            dialog.RestoreDirectory = true;
            dialog.Filter = "yml files (*.yml)|*.yml|All files (*.*)|*.*";
            if (dialog.ShowDialog() == true)
            {
                SolutionInput input = SolutionInput.FromFile(dialog.FileName);
                input.SetInput(parameters);

                if (input.IsFilterGrid)
                {
                    Dependencies.RemoveFilter(); // TODO reduce extra code?
                    FilterGrid filter = new FilterGrid(filterPanel);
                    filter.Set(input.FilterGrid);
                    filterBuilder = filter;
                }
                else
                {
                    Dependencies.RemoveFilter();
                    FilterFormulas filter = new FilterFormulas(filterPanel, Dependencies);
                    filter.Deserialize(input.FilterFormulas);
                    filterBuilder = filter;
                }
            }
        }

        private SolutionInput BuildInput()
        {
            SolutionInput res = new SolutionInput();
            foreach (UIParam param in parameters)
                res.TrySet(param.Name, param.Text);
            res.SetFilter(filterBuilder);
            return res;
        }

        private void AddParam(string name, string label)
        {
            UIParam param = new UIParam(name, label, "NaN");
            paramPanel.Children.Add(param.Panel);
            parameters.Add(param);
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
            SolutionInput input = BuildInput();
            IExpression expr = MainParser.Parse(input.v);
            string[] deps = MainParser.GetDependencies(expr);
            double T = double.Parse(input.T);
            int M = int.Parse(input.t_count);
            int N = int.Parse(input.x_count);
            if (M > VISUALIZATION_HEIGHT)
                M = VISUALIZATION_HEIGHT;
            if (N > VISUALIZATION_WIDTH)
                N = VISUALIZATION_WIDTH;

            if (Dependencies.Get(MathAliases.ConvertName("chi")) == null)
                Dependencies.Set(MathAliases.ConvertName("chi"), 0);

            SwitchDrawButton(true);
            calcBar.Value = 0;
            var calcProgress = new Progress<double>(value => calcBar.Value = value);
            ShowProgress();

            Task.Run(() =>
            {
                AsyncArg arg = new AsyncArg(calcProgress, solveCancellation.Token);
                double[,] sol = MainParser.EvalMatrixD(arg, expr, Dependencies, deps, "t", T, M, "x", 2 * Math.PI, N);
                if (arg.Token.IsCancellationRequested)
                    return;

                this.Dispatcher.Invoke(() =>
                {
                    UpdateVisualization(sol, "U(x,t)");
                    SwitchDrawButton(false);
                });
            });
        }

        private double[] GetExpectedProfile(string vStr)
        {
            SolutionInput input = BuildInput();
            IExpression expr = MainParser.Parse(vStr);
            string[] deps = MainParser.GetDependencies(expr);
            double T = double.Parse(input.T);
            ExprSimplifier.Substitute(expr, "t", new ExprConst(T.ToString("f15")));
            int N = int.Parse(input.x_count);
            if (N > VISUALIZATION_WIDTH)
                N = VISUALIZATION_WIDTH;

            if (Dependencies.Get(MathAliases.ConvertName("chi")) == null)
                Dependencies.Set(MathAliases.ConvertName("chi"), 0);

            return MainParser.EvalArrayD(expr, Dependencies, deps, "x", 2 * Math.PI, N);
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

            await Task.Factory.StartNew(() =>
            {
                AsyncArg asyncArg = new AsyncArg(calcProgress, solveCancellation.Token);
                curSolver.Solve(method, asyncArg);
                if (asyncArg.Token.IsCancellationRequested)
                    return;

                this.Dispatcher.Invoke(() =>
                {
                    textBlock_Khi.Text = "𝜒 = " + curSolver.Chi.ToString("f4");
                    var eigen = FilterInfo.UpdateEigen(filterBuilder.Filter, textBlock_critical, curSolver.Parameters);
                    
                    Complex[] eigenvalies = eigen.Item1;
                    int k = -1;
                    bool isTuring = false;
                    for (int i = 0; i < eigenvalies.Length; i++)
                    {
                        if (eigenvalies[i].Real > 0 || Math.Abs(eigenvalies[i].Real) < 0.005)
                        {
                            if (Math.Abs(eigenvalies[i].Imaginary) < 0.00001)
                            {
                                k = i;
                                isTuring = true;
                                break;
                            }
                            if (eigenvalies[i].Imaginary > 0)
                            {
                                k = i;
                                isTuring = false;
                                break;
                            }
                        }
                    }
                    if (k >= 0)
                    {
                        Complex[] eigenvector = new Complex[eigenvalies.Length];
                        for (int i = 0; i < eigenvector.Length; i++)
                            eigenvector[i] = eigen.Item2[i, k];
                        int center = eigenvector.Length / 2;

                        string v;
                        if (isTuring)
                            v = SolutionInput.GenExpectedTuring(
                                (n) => (eigenvector[center + n] + eigenvector[center - n]).Real,
                                (n) => -(eigenvector[center + n] - eigenvector[center - n]).Imaginary,
                                center, true, 1);
                        else
                            v = SolutionInput.GenExpectedHopf(eigenvalies[k].Imaginary,
                                (n) => (eigenvector[center + n] + eigenvector[center - n]).Real,
                                (n) => -(eigenvector[center + n] - eigenvector[center - n]).Imaginary,
                                (n) => (eigenvector[center + n] + eigenvector[center - n]).Imaginary,
                                (n) => (eigenvector[center + n] - eigenvector[center - n]).Real,
                                center, true, 1);

                        v = RescaleExpectedSolution(v, curSolver.Solution);
                        foreach (UIParam param in parameters)
                            if (param.Name == "v")
                            {
                                param.Text = v;
                                break;
                            }

                        if (isTuring)
                        {
                            double mu1, mu2, s1, s2;
                            {
                                string eigenfunction = v + " - chi";
                                IExpression expr = MainParser.Parse(eigenfunction);
                                string[] deps = MainParser.GetDependencies(expr);
                                double[] ef = MainParser.EvalArrayD(expr, Dependencies, deps, "x", 2 * Math.PI, curSolver.XSize);
                                var Phi_ef = filterBuilder.Filter.Apply_v(curSolver.XSize, ef);
                                double[] ef2 = new double[ef.Length];
                                for (int i = 0; i < ef.Length; i++)
                                    ef2[i] = ef[i] * ef[i];
                                var Phi_ef2 = filterBuilder.Filter.Apply_v(curSolver.XSize, ef2);

                                double[] phiHat = new double[ef.Length];
                                for (int i = 0; i < phiHat.Length; i++)
                                    phiHat[i] = Phi_ef[i].Magnitude * Phi_ef[i].Magnitude - Phi_ef2[i].Real;
                                double dotProd1 = 0;
                                for (int i = 0; i < phiHat.Length; i++)
                                    dotProd1 += phiHat[i] * ef[i] * 1 / phiHat.Length;
                                mu1 = -curSolver.K * curSolver.A0m2 * dotProd1;
                                mu1 = curSolver.K * curSolver.A0m2 * dotProd1;
                            }
                            /*{
                                string eigenfunction = v + " - chi";
                                IExpression expr = MainParser.Parse(eigenfunction);
                                string[] deps = MainParser.GetDependencies(expr);
                                double[] ef = MainParser.EvalArrayD(expr, Dependencies, deps, "x", 2 * Math.PI, curSolver.XSize);

                                // z1 = L~_0 ([Phi1] / [L1 e_n] L1 e_n - Phi1) ...
                                double[] z1 = MainParser.EvalArrayD(expr, Dependencies, deps, "x", 2 * Math.PI, curSolver.XSize);

                                var Phi_ef = filterBuilder.Filter.Apply_v(curSolver.XSize, ef);
                                var Phi_z1 = filterBuilder.Filter.Apply_v(curSolver.XSize, ef);

                                double[] efz1 = new double[ef.Length];
                                for (int i = 0; i < ef.Length; i++)
                                    efz1[i] = ef[i] * z1[i];
                                var Phi_efz1 = filterBuilder.Filter.Apply_v(curSolver.XSize, efz1);

                                double[] phiHat = new double[ef.Length];
                                for (int i = 0; i < phiHat.Length; i++)
                                    phiHat[i] = Phi_ef[i].Magnitude * Phi_ef[i].Magnitude - Phi_efz1[i].Real;
                                double dotProd1 = 0;
                                for (int i = 0; i < phiHat.Length; i++)
                                    dotProd1 += phiHat[i] * ef[i] * 1 / phiHat.Length;
                                s1 = dotProd1;
                            }
                            {
                                string eigenfunction = v + " - chi";
                                IExpression expr = MainParser.Parse(eigenfunction);
                                string[] deps = MainParser.GetDependencies(expr);
                                double[] ef = MainParser.EvalArrayD(expr, Dependencies, deps, "x", 2 * Math.PI, curSolver.XSize);
                                var Phi_ef = filterBuilder.Filter.Apply_v(curSolver.XSize, ef);
                                double[] ef2 = new double[ef.Length];
                                for (int i = 0; i < ef.Length; i++)
                                    ef2[i] = ef[i] * ef[i];
                                var Phi_ef2 = filterBuilder.Filter.Apply_v(curSolver.XSize, ef2);

                                double[] phiHat = new double[ef.Length];
                                for (int i = 0; i < phiHat.Length; i++)
                                    phiHat[i] = Phi_ef[i].Magnitude * Phi_ef[i].Magnitude - Phi_ef2[i].Real;
                                double dotProd1 = 0;
                                for (int i = 0; i < phiHat.Length; i++)
                                    dotProd1 += phiHat[i] * ef[i] * 1 / phiHat.Length;
                                double dotProd2 = 1;
                                mu2 = -curSolver.K * curSolver.A0m2 * dotProd1 / dotProd2;
                            }
                            {
                                string eigenfunction = v + " - chi";
                                IExpression expr = MainParser.Parse(eigenfunction);
                                string[] deps = MainParser.GetDependencies(expr);
                                double[] ef = MainParser.EvalArrayD(expr, Dependencies, deps, "x", 2 * Math.PI, curSolver.XSize);
                                var Phi_ef = filterBuilder.Filter.Apply_v(curSolver.XSize, ef);
                                double[] ef2 = new double[ef.Length];
                                for (int i = 0; i < ef.Length; i++)
                                    ef2[i] = ef[i] * ef[i];
                                var Phi_ef2 = filterBuilder.Filter.Apply_v(curSolver.XSize, ef2);

                                double[] phiHat = new double[ef.Length];
                                for (int i = 0; i < phiHat.Length; i++)
                                    phiHat[i] = Phi_ef[i].Magnitude * Phi_ef[i].Magnitude - Phi_ef2[i].Real;
                                double dotProd1 = 0;
                                for (int i = 0; i < phiHat.Length; i++)
                                    dotProd1 += phiHat[i] * ef[i] * 1 / phiHat.Length;
                                double dotProd2 = 1;
                                s2 = -curSolver.K * curSolver.A0m2 * dotProd1 / dotProd2;
                            }*/


                            textBlock_Khi.Text = "phi1 = " + mu1.ToString("f4");/* + " "
                                + "phi2 = " + mu2.ToString("f4") + " "
                                + "phi3 = " + s1.ToString("f4") + " "
                                + "phi4 = " + s2.ToString("f4") + " ";*/
                        }
                    }

                    UpdateVisualization(curSolver.Solution, "u(x,T)");
                    SwitchDrawButton(false);
                });
            });
        }

        private string RescaleExpectedSolution(string v, double[,] solution)
        {
            double maxValue = 0;
            int maxValueIndex = 0;
            for (int j = 0; j < solution.GetLength(1); j++)
            {
                double value = solution[solution.GetLength(0) - 1, j];
                if (value > maxValue)
                {
                    maxValueIndex = j;
                    maxValue = value;
                }
            }
            double[] expectedSolution = GetExpectedProfile(v);
            double maxValue2 = 0;
            int maxValueIndex2 = 0;
            for (int j = 0; j < expectedSolution.Length; j++)
            {
                double value = expectedSolution[j];
                if (value > maxValue2)
                {
                    maxValueIndex2 = j;
                    maxValue2 = value;
                }
            }
            double amplCoef = (maxValue - curSolver.Chi) / (maxValue2 - curSolver.Chi);
            if (maxValueIndex != maxValueIndex2)
            {
                if (Math.Abs(maxValueIndex - maxValueIndex2) > 10)
                {
                    amplCoef *= -1;
                }
                else
                {
                    string deltaPhiStr = "2pi*(" + (maxValueIndex2 - maxValueIndex) + "/" + expectedSolution.Length + ")";
                    v = v.Replace("x", "(x + " + deltaPhiStr + ")");
                }
            }
            return v.Substring(0, 3) + " + " + amplCoef.ToString("f10") + "(" + v.Substring(3) + ")";
        }

        private void BuildSolver()
        {
            SolutionInput input = BuildInput();
            Filter P = input.Filter;

            Solver newSolver;
            string errorElem = "";
            try
            {
                int n = P.Size;
                errorElem = "D";
                double D = double.Parse(input.D);
                errorElem = "A0";
                Complex A0 = ComplexUtils.Parse(input.A_0);
                errorElem = "K";
                double K = double.Parse(input.K);
                errorElem = "T";
                double T = double.Parse(input.T);
                errorElem = "M";
                int M = int.Parse(input.t_count);
                errorElem = "N";
                int N = int.Parse(input.x_count);

                double chi = Solver.GetChi(K, P[n, n], A0);
                Dependencies.Set(MathAliases.ConvertName("chi"), chi);

                errorElem = "u0";
                IExpression expr = MainParser.Parse(input.u_0);
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

        private void UpdateVisualization(double[,] solution, string name)
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

            SavePlotAndProfiles(solution);

            RunPlotWindow(GetLastLayer(solution), name);
        }

        private void SavePlotAndProfiles(double[,] solution)
        {
            string extension = ".txt";
            string xLength = "2*pi";
            string name = DateTime.Now.ToString("yyyy'-'MM'-'dd'_'HH'-'mm'-'ss");
            SolutionInput input = BuildInput();
            save2D(name + extension, input.T, xLength, solution);
            double[] solutionProfile = new double[solution.GetLength(1)];
            for (int i = 0; i < solutionProfile.Length; i++)
            {
                solutionProfile[i] = solution[solution.GetLength(0) - 1, i];
            }
            double[] expectedProfile = GetExpectedProfile(input.v);
            save1D(name + "_profiles" + extension, xLength, "u(x, T)", solutionProfile, "U(x, t)", expectedProfile);
        }

        private void save1D(string filename, string xLength, string p1Name, double[] p1, string p2Name, double[] p2)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                int size = p1.Length;
                writer.WriteLine("1D");
                writer.WriteLine(size + " " + xLength + " x");
                writer.WriteLine("u");
                writer.WriteLine(p1Name);
                writer.WriteLine("#00FF00");
                string line = "";
                for (int i = 0; i < size; i++)
                {
                    if (line.Length > 0)
                        line += " ";
                    line += p1[i];
                }
                writer.WriteLine(line);
                writer.WriteLine(p2Name);
                writer.WriteLine("#0000FF");
                line = "";
                for (int i = 0; i < size; i++)
                {
                    if (line.Length > 0)
                        line += " ";
                    line += p2[i];
                }
                writer.WriteLine(line);
            }
        }

        private void save2D(string filename, string tLength, string xLength, double[,] data)
        {
            using (StreamWriter writer = new StreamWriter(filename))
            {
                writer.WriteLine("2D");
                writer.WriteLine(data.GetLength(0) + " " + tLength + " T");
                writer.WriteLine(data.GetLength(1) + " " + xLength + " x");
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    string line = "";
                    for (int i = 0; i < data.GetLength(0); i++)
                    {
                        if (line.Length > 0)
                            line += " ";
                        line += data[i, j];
                    }
                    writer.WriteLine(line);
                }
            }
        }

        private double[] GetLastLayer(double[,] solution)
        {
            int lastLayer = solution.GetLength(0) - 1;
            if (lastLayer < 0)
                return new double[0];

            double[] res = new double[solution.GetLength(1)];
            for (int i = 0; i < res.Length; i++)
                res[i] = solution[lastLayer, i];
            return res;
        }

        private void RunPlotWindow(double[] values, string title)
        {
            if (plotWindow == null || !plotWindow.IsLoaded) // https://stackoverflow.com/questions/381973/how-do-you-tell-if-a-wpf-window-is-closed
            {
                plotWindow = new PlotWindow();
                plotWindow.Owner = this;
            }

            plotWindow.DrawLinePlot(0, 2 * Math.PI, values, title);

            plotWindow.Show();
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
