
using HeatSim;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Bifurcation
{
    static class MainParser
    {
        private static readonly ExprParser Parser;

        static MainParser()
        {
            Parser = new ExprParser();
            Parser.AddAliases(MathAliases.GetDefaultFunctions());
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
            //replace, considering functions
            return 0;
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
    }
}
