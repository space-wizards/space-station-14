using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Content.Starlight.Codegen;

[Generator]
public sealed class LocalSubscriptionsGenerator : IIncrementalGenerator
{
    private const string AttributeName = "Content.Shared.Starlight.Abstract.Codegen.GenerateLocalSubscriptionsAttribute`1";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classDeclarations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AttributeName,
                (node, _) => node is ClassDeclarationSyntax,
                (ctx, _) => (Class: (INamedTypeSymbol)ctx.TargetSymbol, Attribute: ctx.Attributes[0]))
            .Collect();

        var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations);

        context.RegisterSourceOutput(compilationAndClasses,
            (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private void Execute(SourceProductionContext context, Compilation compilation, ImmutableArray<(INamedTypeSymbol Class, AttributeData Attribute)> classes)
    {
        if (classes.IsDefaultOrEmpty)
            return;

        var componentType = compilation.GetTypeByMetadataName("Robust.Shared.GameObjects.IComponent");
        if (componentType == null)
            return;

        var allComponents = GetAllComponents(compilation, componentType);

        foreach (var (classSymbol, attribute) in classes)
        {
            var source = GenerateSubscriptionClass(compilation, classSymbol, attribute, allComponents);
            if (source != null)
            {
                context.AddSource($"{classSymbol.Name}.Subscriptions.g.cs", source);
            }
        }
    }

    private static List<INamedTypeSymbol> GetAllComponents(Compilation compilation, INamedTypeSymbol componentType)
    {
        var components = new List<INamedTypeSymbol>();
        var allTypes = GetAllTypes(compilation.GlobalNamespace);

        foreach (var type in allTypes)
        {
            if (type.TypeKind == TypeKind.Class && !type.IsAbstract && type.AllInterfaces.Contains(componentType, SymbolEqualityComparer.Default))
            {
                components.Add(type);
            }
        }
        return components;
    }

    private string? GenerateSubscriptionClass(Compilation compilation, INamedTypeSymbol classSymbol, AttributeData attribute, List<INamedTypeSymbol> allComponents)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {classSymbol.ContainingNamespace.ToDisplayString()}");
        sb.AppendLine("{");
        sb.AppendLine($"    public partial class {classSymbol.Name}");
        sb.AppendLine("    {");

        if (attribute.AttributeClass?.TypeArguments.Length != 1)
            return null;

        var interfaceType = attribute.AttributeClass.TypeArguments[0];
        var componentsToSubscribe = allComponents
            .Where(c => c.AllInterfaces.Contains(interfaceType, SymbolEqualityComparer.Default))
            .ToList();

        GenerateSubscriptionMethod(sb, interfaceType, componentsToSubscribe);

        sb.AppendLine("    }");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private void GenerateSubscriptionMethod(StringBuilder sb, ITypeSymbol interfaceType, List<INamedTypeSymbol> components)
    {
        var interfaceName = interfaceType.Name.StartsWith("I") ? interfaceType.Name.Substring(1) : interfaceType.Name;

        sb.AppendLine($"        public void SubscribeAll{interfaceName}<TEvent>(EntityEventRefHandler<{interfaceType.ToDisplayString()}, TEvent> handler) where TEvent : notnull");
        sb.AppendLine("        {");

        foreach (var component in components)
        {
            var componentName = component.Name.Replace(".", "_");
            sb.AppendLine($"            SubscribeLocalEvent<{component.ToDisplayString()}, TEvent>({componentName}Handler);");
            sb.AppendLine($"            void {componentName}Handler(Entity<{component.ToDisplayString()}> ent, ref TEvent ev)");
            sb.AppendLine($"                => handler((ent.Owner, ent.Comp), ref ev);");
        }

        sb.AppendLine("        }");
    }

    private static IEnumerable<INamedTypeSymbol> GetAllTypes(INamespaceSymbol root)
    {
        foreach (var namespaceOrType in root.GetMembers())
        {
            if (namespaceOrType is INamespaceSymbol @namespace)
            {
                foreach (var nested in GetAllTypes(@namespace))
                {
                    yield return nested;
                }
            }
            else if (namespaceOrType is INamedTypeSymbol type)
            {
                yield return type;
            }
        }
    }
}
