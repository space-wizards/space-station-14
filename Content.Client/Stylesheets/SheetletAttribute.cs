using System.Linq;

namespace Content.Client.Stylesheets;

/// <summary>
/// Attribute used to mark a sheetlet class, used to locate, verify constraints, and then generate stylesheets via reflection.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SheetletAttribute : Attribute
{
    /// <summary>
    /// Stylesheet factories to run generate for.
    /// </summary>
    /// <remarks>
    /// This provides the ability to conditionally apply sheetlets on certain factories, even if they implement the
    /// required sheetlet configs.
    /// </remarks>
    public Type[] Factories { get; }

    /// <summary>
    /// Attribute used to mark a sheetlet class. Stylesheets can use this attribute to locate and load sheetlets.
    /// </summary>
    /// <param name="factory">First factory to match</param>
    /// <param name="factories">Stylesheet factories to generate for.</param>
    /// <exception cref="ArgumentException">If the type provided is not a <see cref="StylesheetFactory"/> </exception>
    public SheetletAttribute(Type factory, params Type[] factories)
    {
        var fs = factories.ToList();
        // Used to stop people from providing 0 factories w/o requiring custom Roslyn analyzers.
        fs.Add(factory);

        foreach (var f in fs)
        {
            if (!typeof(StylesheetFactory).IsAssignableFrom(f))
                throw new ArgumentException($"{factory} is not a {nameof(StylesheetFactory)}");
        }

        Factories = factories;
    }
}
