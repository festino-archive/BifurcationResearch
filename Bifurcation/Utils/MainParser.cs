
using HeatSim;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace Bifurcation
{
    static class MainParser
    {
        private static readonly ExprParser Parser;

        static MainParser()
        {
            Parser = new ExprParser();
            Parser.AddAliases(MathAliases.GetDefaultFunctions());
            Parser.AddAlias(MathAliases.ConvertName("i"), 0);
            Parser.AddAlias(MathAliases.ConvertName("t"), 0);
            Parser.AddAlias(MathAliases.ConvertName("x"), 0);
            Parser.AddAlias(MathAliases.ConvertName("chi"), 0);
            Parser.AddAlias(MathAliases.ConvertName("rho"), 2);
            Parser.AddAlias(MathAliases.ConvertName("P"), 2);
        }

        public static IExpression Parse(string formula)
        {
            IExpression expr = Parser.Parse(formula);
            expr = ExprSimplifier.Simplify(expr);
            return expr;
        }

        public static string[] GetDependencies(IExpression init)
        {
            HashSet<string> deps = new HashSet<string>();
            foreach (IExpression expr in init.GetArgs())
            {
                if (expr is ExprRegFunc)
                {
                    ExprRegFunc func = expr as ExprRegFunc;
                    if (!IsDefaultFunction(func))
                        deps.Add(MathAliases.ConvertName(func.Name));
                }
                if (expr.GetArgsCount() > 0)
                {
                    string[] internalDeps = GetDependencies(expr);
                    foreach (string dep in internalDeps)
                        deps.Add(dep);
                }
            }
                
            return deps.ToArray();
        }

        private static bool IsDefaultFunction(ExprRegFunc func)
        {
            string name = func.Name;
            if (name == "ln" || name == "exp" || name == "abs"
                    || name == "sin" || name == "cos" || name == "tg" || name == "tan" || name == "ctg" || name == "cot"
                    || name == "arcsin" || name == "arccos" || name == "arctg" || name == "arctan" || name == "arcctg" || name == "arccot")
                return true;
            return false;
        }

        public static Complex Eval(IExpression expr, List<DependencyNode> dependencies)
        {
            expr = ExprUtils.GetCopy_Slow(expr);
            foreach (DependencyNode dep in dependencies)
            {
                Complex val = dep.Value;
                IExpression constExpr;
                if (val.Imaginary == 0)
                {
                    constExpr = new ExprConst(val.Real.ToString("f15"));
                }
                else
                {
                    IExpression exprImg = new ExprMult(new ExprConst(val.Real.ToString("f15")), new ExprRegFunc("i", 0));
                    if (val.Real == 0)
                        constExpr = exprImg;
                    else
                        constExpr = new ExprSum(new ExprConst(val.Real.ToString("f15")), exprImg);
                }
                expr = ExprSimplifier.Substitute(expr, dep.Name, constExpr);
            }
            expr = ExprSimplifier.Simplify(expr);

            //deal with static dependencies(chi?)
            //replace, considering functions

            if (expr is ExprConst)
            {
                return (expr as ExprConst).Value.ToDouble();
            }
            else if (expr is ExprMult)
            {
                ExprMult mult = expr as ExprMult;
                double img = 0;
                bool found = false;
                foreach (IExpression e in mult.GetArgs())
                    if (e is ExprConst)
                        img = (e as ExprConst).Value.ToDouble();
                    else if (e is ExprRegFunc && (e as ExprRegFunc).Name == "i")
                        found = true;
                if (found)
                    return new Complex(0, img);
            }
            else if (expr is ExprSum)
            {
                ExprSum sum = expr as ExprSum;
                bool foundReal = false;
                bool foundImg = false;
                double real = 0;
                double img = 0;
                foreach (IExpression eSum in sum.GetArgs())
                    if (eSum is ExprConst)
                    {
                        real = (eSum as ExprConst).Value.ToDouble();
                        foundReal = true;
                    }
                    else if (eSum is ExprMult)
                    {
                        ExprMult mult = eSum as ExprMult;
                        foreach (IExpression e in mult.GetArgs())
                            if (e is ExprConst)
                                img = (e as ExprConst).Value.ToDouble();
                            else if (e is ExprRegFunc && (e as ExprRegFunc).Name == "i")
                                foundImg = true;
                    }
                if (foundReal && foundImg)
                    return new Complex(real, img);
            }
            return Complex.NaN;
        }

        public static double EvalD(IExpression expr, List<DependencyNode> dependencies)
        {
            return Eval(expr, dependencies).Real;
        }

        public static double[] EvalArrayD(IExpression expr, DependencySpace depSpace, string[] deps, string varName, double length, int sections)
        {
            expr = ExprUtils.GetCopy_Slow(expr);
            foreach (string dep in deps)
            {
                Complex? val = depSpace.Get(dep);
                if (val.HasValue)
                    expr = ExprSimplifier.Substitute(expr, dep, new ExprConst(val.Value.Real.ToString("f15")));
            }
            double[] u0 = new double[sections];
            for (int j = 0; j < u0.Length; j++)
            {
                double v = length * j / u0.Length;
                IExpression substituted = ExprSimplifier.Substitute(expr, varName, new ExprConst(v.ToString("f15")));
                substituted = ExprSimplifier.Simplify(substituted);
                u0[j] = ExprDoubleSimplifier.CalcConstExpr(substituted);
            }
            return u0;
        }

        public static double[,] EvalMatrixD_OneThread(AsyncArg arg, IExpression expr, DependencySpace depSpace, string[] deps,
            string var1, double len1, int steps1, string var2, double len2, int steps2)
        {
            expr = ExprUtils.GetCopy_Slow(expr);
            foreach (string dep in deps)
            {
                Complex? val = depSpace.Get(dep);
                if (val.HasValue)
                    expr = ExprSimplifier.Substitute(expr, dep, new ExprConst(val.Value.Real.ToString("f15")));
            }
            double[,] res = new double[steps1, steps2]; // t, x
            for (int n = 0; n < steps1; n++)
            {
                if (arg.Token.IsCancellationRequested)
                    return null;

                double v1 = len1 * n / steps1;
                IExpression substituted1 = ExprSimplifier.Substitute(expr, var1, new ExprConst(v1.ToString("f15")));
                for (int j = 0; j < steps2; j++)
                {
                    double v2 = len2 * j / steps2;
                    IExpression substituted2 = ExprSimplifier.Substitute(substituted1, var2, new ExprConst(v2.ToString("f15")));
                    substituted2 = ExprSimplifier.Simplify(substituted2);
                    res[n, j] = ExprDoubleSimplifier.CalcConstExpr(substituted2);
                }

                arg.Progress?.Report((n + 1) / (float)steps1);
            }
            return res;
        }

        public static double[,] EvalMatrixD(AsyncArg arg, IExpression expr, DependencySpace depSpace, string[] deps,
            string var1, double len1, int steps1, string var2, double len2, int steps2)
        {
            expr = ExprUtils.GetCopy_Slow(expr);
            foreach (string dep in deps)
            {
                Complex? val = depSpace.Get(dep);
                if (val.HasValue)
                    expr = ExprSimplifier.Substitute(expr, dep, new ExprConst(val.Value.Real.ToString("f15")));
            }
            double[,] res = new double[steps1, steps2]; // t, x
            int[] progress = new int[Environment.ProcessorCount];
            int interval = (int)Math.Ceiling(steps1 / (double)progress.Length);
            Parallel.For(0, progress.Length, (index) => EvalMatrixD(arg, expr, res, steps1, progress, index,
                var1, len1 / steps1, index * interval, Math.Min((index + 1) * interval, steps1), var2, len2 / steps2, steps2));
            return res;
        }

        private static void EvalMatrixD(AsyncArg arg, IExpression exprV1V2, double[,] res, int steps1, int[] progress, int index,
            string var1, double step1, int start1, int end1, string var2, double step2, int steps2)
        {
            for (int n = start1; n < end1; n++)
            {
                if (arg.Token.IsCancellationRequested)
                    return;

                double v1 = step1 * n;
                IExpression substituted1 = ExprSimplifier.Substitute(exprV1V2, var1, new ExprConst(v1.ToString("f15")));
                for (int j = 0; j < steps2; j++)
                {
                    double v2 = step2 * j;
                    IExpression substituted2 = ExprSimplifier.Substitute(substituted1, var2, new ExprConst(v2.ToString("f15")));
                    substituted2 = ExprSimplifier.Simplify(substituted2);
                    res[n, j] = ExprDoubleSimplifier.CalcConstExpr(substituted2);
                }

                lock (progress)
                {
                    progress[index]++;
                    int steps = 0;
                    foreach (int s in progress)
                        steps += s;
                    arg.Progress?.Report(steps / (float)steps1);
                }
            }
        }
    }
}
