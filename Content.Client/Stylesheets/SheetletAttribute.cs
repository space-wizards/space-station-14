using System.Linq;

namespace Content.Client.Stylesheets;

/// <summary>
/// Attribute used to mark a sheetlet class, used to locate, verify constraints, and then generate stylesheets via reflection.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SheetletAttribute : Attribute
{
    /// <summary>
    /// Stylesheet definitions to run generate for.
    /// </summary>
    /// <remarks>
    /// This provides the ability to conditionally apply sheetlets on certain definitions, even if they implement the
    /// required sheetlet configs.
    /// </remarks>
    public Type[] Definitions { get; }

    /// <summary>
    /// Attribute used to mark a sheetlet class. Stylesheets can use this attribute to locate and load sheetlets.
    /// </summary>
    /// <param name="definition">First definition to match</param>
    /// <param name="definitions">Stylesheet definitions to generate for.</param>
    /// <exception cref="ArgumentException">If the type provided is not a <see cref="StylesheetDefinition"/> </exception>
    public SheetletAttribute(Type definition, params Type[] definitions)
    {
        var ds = definitions.ToList();
        // Used to stop people from providing 0 definitions w/o requiring custom Roslyn analyzers.
        ds.Add(definition);

        foreach (var d in ds)
        {
            if (!typeof(StylesheetDefinition).IsAssignableFrom(d))
                throw new ArgumentException($"{d} is not a {nameof(StylesheetDefinition)}");
        }

        Definitions = ds.ToArray();
    }
}
