using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynSourceGeneratorNameof
{
    [Generator]
    public class MyBuggyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForPostInitialization(postCtx =>
            {
            });
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.Compilation is not CSharpCompilation compilation || 
                context.SyntaxContextReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            var notNull = compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.MemberNotNullAttribute");

            foreach (var targetClass in receiver.Types)
            {
                string className = targetClass.Name;
                string access = targetClass.DeclaredAccessibility.ToString().ToLower();

                IMethodSymbol[] methods = targetClass.GetMembers().OfType<IMethodSymbol>().ToArray();

                foreach (IMethodSymbol method in methods)
                {
                    if (method.GetAttributes().FirstOrDefault(ad => SymbolEqualityComparer.Default.Equals(ad.AttributeClass, notNull)) is AttributeData ad)
                    {
                        ImmutableArray<TypedConstant> args =  ad.ConstructorArguments; // actual {System.Diagnostics.CodeAnalysis.MemberNotNullAttribute("")}
                        if (args[0].Value is "")                                       // should be {System.Diagnostics.CodeAnalysis.MemberNotNullAttribute("GeneratedProp")}
                        {
                            throw new Exception("here's bug!");
                        }
                    }
                }

                context.AddSource(className + "_generated.cs", @$"
                partial class {className}
                {{
                    public int GeneratedProp {{ get; set; }}
                }}");
            }
        }
    }

    internal class SyntaxReceiver : ISyntaxContextReceiver
    {
        public List<INamedTypeSymbol> Types { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is ClassDeclarationSyntax cds)
            {
                ISymbol? symbol = context.SemanticModel.GetDeclaredSymbol(cds);
                if (symbol is INamedTypeSymbol typeSymbol)
                {
                    Types.Add(typeSymbol);
                }
            }
        }
    }
}
