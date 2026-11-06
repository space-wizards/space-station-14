using CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.IO;

namespace CVarDocGen;

public class Options
{
    [Option('s', "src", Required = true, HelpText = "Paths to the folders containing CCVar C# files. Repeatable.")]
    public IEnumerable<string> SourcePaths { get; set; } = Array.Empty<string>();

    [Option('o', "out", Required = true, HelpText = "Path to the output folder for markdown files.")]
    public string OutputPath { get; set; } = "";
}

public class CVarInfo
{
    public string DeclaratorName { get; set; } = "";
    public string CVarName { get; set; } = "";
    public string Type { get; set; } = "";
    public string DefaultValue { get; set; } = "";
    public string Description { get; set; } = "";
    public string Flags { get; set; } = "";
    public string XmlDoc { get; set; } = "";
    public string Category { get; set; } = "Uncategorized";
    public string Scope { get; set; } = "Both";
}

public static class Program
{
    public static void Main(string[] args)
    {
        var parser = new Parser(with => with.AllowMultiInstance = true);
        parser.ParseArguments<Options>(args)
            .WithParsed(RunOptions);
    }

    static void RunOptions(Options opts)
    {
        if (!Directory.Exists(opts.OutputPath))
            Directory.CreateDirectory(opts.OutputPath);

        var bigBuilder = new StringBuilder();
        bigBuilder.AppendLine("# Console Variables (CCVars)");
        bigBuilder.AppendLine();
        bigBuilder.AppendLine("This page aggregates all CCVars defined in the repository. Use the table below to find the setting you need.");
        bigBuilder.AppendLine();

        var sourceFiles = new List<string>();
        foreach (var src in opts.SourcePaths)
        {
            if (Directory.Exists(src))
            {
                var files = Directory.GetFiles(src, "*CVars*.cs", SearchOption.AllDirectories);
                sourceFiles.AddRange(files);
                Console.WriteLine($"Found {files.Length} files in {src}");
            }
            else
            {
                Console.WriteLine($"Warning: source path does not exist: {src}");
            }
        }

        sourceFiles = sourceFiles.Distinct().ToList();
        Console.WriteLine($"Total source files: {sourceFiles.Count}");

        var allCvars = new List<CVarInfo>();

        foreach (var filePath in sourceFiles.OrderBy(f => Path.GetFileName(f)))
        {
            var fileContent = File.ReadAllText(filePath);

            var tree = CSharpSyntaxTree.ParseText(fileContent);
            var root = tree.GetCompilationUnitRoot();

            var fieldDeclarations = root.DescendantNodes().OfType<FieldDeclarationSyntax>();

            foreach (var field in fieldDeclarations)
            {
                if (field.Declaration.Type is GenericNameSyntax genericType &&
                    genericType.Identifier.Text == "CVarDef")
                {
                    var typeArg = genericType.TypeArgumentList.Arguments.FirstOrDefault()?.ToString() ?? "unknown";

                    foreach (var variable in field.Declaration.Variables)
                    {
                        var cvarNameDeclarator = variable.Identifier.Text;
                        var info = new CVarInfo
                        {
                            DeclaratorName = cvarNameDeclarator,
                            Type = typeArg
                        };

                        var leadingTrivia = field.GetLeadingTrivia();
                        var structure = leadingTrivia.Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().FirstOrDefault();
                        if (structure != null)
                        {
                            var summary = structure.Content.OfType<XmlElementSyntax>()
                                .FirstOrDefault(e => e.StartTag.Name.ToString() == "summary");

                            if (summary != null)
                            {
                                info.XmlDoc = GetXmlContent(summary).Trim();
                            }
                        }

                        if (variable.Initializer?.Value is InvocationExpressionSyntax invocation &&
                            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                            memberAccess.Name.Identifier.Text == "Create")
                        {
                            var args = invocation.ArgumentList.Arguments;

                            // Arg 0: Name
                            if (args.Count >= 1) info.CVarName = args[0].Expression.ToString().Trim('"');

                            // Arg 1: Default Value
                            if (args.Count >= 2) info.DefaultValue = args[1].Expression.ToString();

                            // Arg 2: Flags (Optional)
                            if (args.Count >= 3) info.Flags = args[2].Expression.ToString();

                            // Arg 3: Description (Optional)
                            if (args.Count >= 4) info.Description = args[3].Expression.ToString().Trim('"');
                        }
                        else
                        {
                            info.CVarName = cvarNameDeclarator;
                        }

                        // Determine Category
                        var parts = info.CVarName.Split('.');
                        if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]))
                        {
                            info.Category = parts[0];
                        }

                        // Determine Scope
                        if (info.Flags.Contains("SERVERONLY"))
                            info.Scope = "Server";
                        else if (info.Flags.Contains("CLIENTONLY"))
                            info.Scope = "Client";
                        else
                            info.Scope = "Both";

                        allCvars.Add(info);
                    }
                }
            }
        }

        // Group by Category and generate tables
        var grouped = allCvars.GroupBy(x => x.Category).OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var categoryName = char.ToUpper(group.Key[0]) + group.Key.Substring(1); // Capitalize first letter
            bigBuilder.AppendLine($"## {categoryName}");
            bigBuilder.AppendLine();

            bigBuilder.AppendLine("| Name | Type | Default | Scope | Description |");
            bigBuilder.AppendLine("|---|---|---|---|---|");

            foreach (var cvar in group.OrderBy(c => c.CVarName))
            {
                var desc = !string.IsNullOrWhiteSpace(cvar.Description) ? cvar.Description : cvar.XmlDoc;
                desc = desc.Replace("|", "\\|").Replace("\n", " ");
                var def = cvar.DefaultValue.Replace("|", "\\|");
                bigBuilder.AppendLine($"| `{cvar.CVarName}` | `{cvar.Type}` | `{def}` | {cvar.Scope} | {desc} |");
            }
            bigBuilder.AppendLine();
            Console.WriteLine($"Appended Category {categoryName} ({group.Count()} cvars)");
        }

        // Write combined CCVars page
        var combinedPath = Path.Combine(opts.OutputPath, "ccvars.md");
        File.WriteAllText(combinedPath, bigBuilder.ToString());
        Console.WriteLine($"Generated {combinedPath}");
    }

    static string GetXmlContent(XmlElementSyntax element)
    {
        var sb = new StringBuilder();
        foreach (var node in element.Content)
        {
            if (node is XmlTextSyntax text)
            {
                foreach (var token in text.TextTokens)
                {
                    sb.Append(token.Text);
                }
            }
            else if (node is XmlEmptyElementSyntax empty && empty.Name.ToString() == "see")
            {
                var cref = empty.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault();
                if (cref != null)
                {
                    sb.Append($"`{cref.Cref}`");
                }
            }
            else if (node is XmlElementSyntax child)
            {
                sb.Append(GetXmlContent(child));
            }
        }

        // Remove /// comments from raw text if any slipped through, split lines, trim, join
        var lines = sb.ToString()
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim().TrimStart('/').Trim());

        return string.Join(" ", lines);
    }
}
