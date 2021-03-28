
using HeatSim;
using System.Collections.Generic;

namespace Bifurcation
{
    static class FilterFormulaParser
    {
        private static readonly ExprParser Parser;

        static FilterFormulaParser()
        {
            Parser = new ExprParser();
            Parser.AddAliases(MathAliases.GetDefaultFunctions());
            Parser.AddAlias(MathAliases.ConvertName("x"), 0);
        }

        public static double Parse(int k, int n, string formula, List<FormulaSlot> formulas)
        {
            return 0;
        }
    }
}
