using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeGen
{
    public static class Utility
    {
        public static string GetPayloadTableClassName(InterfaceDeclarationSyntax idecl)
        {
            return idecl.Identifier + "_PayloadTable";
        }

        public static string GetServerEntityBaseClassName(InterfaceDeclarationSyntax idecl)
        {
            return idecl.Identifier.ToString().Substring(1) + "ServerBase";
        }

        public static string GetClientEntityBaseClassName(InterfaceDeclarationSyntax idecl)
        {
            return idecl.Identifier.ToString().Substring(1) + "ClientBase";
        }
    }
}
