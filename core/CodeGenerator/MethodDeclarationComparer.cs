using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen
{
    public class MethodDeclarationSyntaxComparer : IComparer<MethodDeclarationSyntax>
    {
        public int Compare(MethodDeclarationSyntax x, MethodDeclarationSyntax y)
        {
            var ret = string.Compare(x.Identifier.Text, y.Identifier.Text, StringComparison.Ordinal);
            if (ret != 0)
                return ret;

            var xp = x.ParameterList.Parameters;
            var yp = y.ParameterList.Parameters;
            for (var i = 0; i < Math.Min(xp.Count, yp.Count); i++)
            {
                var ret2 = string.Compare(xp[i].Identifier.Text, yp[i].Identifier.Text, StringComparison.Ordinal);
                if (ret2 != 0)
                    return ret2;

                var ret3 = string.Compare(xp[i].Type.ToFullString(), yp[i].Type.ToFullString(), StringComparison.Ordinal);
                if (ret3 != 0)
                    return ret3;
            }

            return xp.Count - yp.Count;
        }
    }
}
