using System.Linq;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Reflection;
using Robust.Shared.Sandboxing;
using static Robust.Shared.Utility.TypeHelpers;

namespace Content.Client.Stylesheets;

/// <summary>
/// Style factories aggregate sheetlets together, provide resource resolution functionality, and create a stylesheet.
/// </summary>
public abstract partial class StylesheetFactory : ISheetletConfig
{
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private ISandboxHelper _sandboxHelper = default!;
    [Dependency] private IReflectionManager _reflectionManager = default!;

    protected StylesheetFactory()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Builds the style rules from a specified sheetlet type.
    /// </summary>
    /// <param name="sheetletType">Type of the sheetlet to instantiate.</param>
    /// <returns>Sheetlet's style rules, or nothing if the attribute is not set for the factory type.</returns>
    /// <exception cref="ArgumentException">Missing Sheetlet attribute.</exception>
    /// <exception cref="MissingSheetletConstraintsException">A factory is marked for a sheetlet yet can't meet the constraints.</exception>
    /// <exception cref="Exception">Sandbox instantiation exceptions.</exception>
    private StyleRule[] BuildSheetlet(Type sheetletType)
    {
        if (!sheetletType.TryGetCustomAttribute<SheetletAttribute>(out var attribute))
            throw new ArgumentException($"Type '{sheetletType}' does not have Sheetlet attribute.");

        if (!attribute.Factories.Any(f => f.IsInstanceOfType(this)))
            return [];

        Type sheetletClosedType;
        try
        {
            sheetletClosedType = sheetletType.MakeGenericType(GetType());
        }
        catch (ArgumentException)
        {
            var diff = sheetletType.GetGenericArguments()
                .Single()
                .GetGenericParameterConstraints()
                .Where(t => !t.IsInstanceOfType(this))
                .ToArray();
            throw new MissingSheetletConstraintsException(this, sheetletType, diff);
        }

        return _sandboxHelper.CreateInstance(sheetletClosedType) is not ISheetlet<ISheetletConfig> sheetlet
            ? throw new Exception($"Failed to create instance of sheetlet type {sheetletClosedType}.")
            : sheetlet.GetRules(this, this);
    }

    /// <summary>
    /// Builds the stylesheet.
    /// </summary>
    /// <returns>Stylesheet constructed from all the sheetlets.</returns>
    public Stylesheet Build()
    {
        // TODO: sort the sheetlet types so that types closer inheritance/relationally go after ones that are further,
        // so they can "override"/act as some sort of "ordering".
        var sheetletTypes = _reflectionManager.FindTypesWithAttribute<SheetletAttribute>();
        var rules = new List<StyleRule>();

        foreach (var sheetletType in sheetletTypes)
        {
            rules.AddRange(BuildSheetlet(sheetletType));
        }

        return new Stylesheet(rules.ToArray());
    }
}

public sealed class MissingSheetletConstraintsException(
    StylesheetFactory factory,
    Type sheetlet,
    Type[] configs) : Exception
{
    public override string Message =>
        $"Stylesheet factory {factory} is marked as a factory for sheetlet {sheetlet}, yet is missing sheetlet configs {string.Join(", ", configs)}";

    public override string? Source => factory.ToString();
}
