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
    [Dependency] private ILogManager _logManager = default!;

    private readonly ISawmill _sawmill;

    protected StylesheetFactory()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _logManager.GetSawmill("style");
    }

    /// <summary>
    /// Builds the style rules from a specified sheetlet type.
    /// </summary>
    /// <param name="sheetletType">Type of the sheetlet to instantiate.</param>
    /// <returns>Sheetlet's style rules, or nothing if the attribute is not set for the factory type.</returns>
    /// <exception cref="ArgumentException">Missing Sheetlet attribute.</exception>
    /// <exception cref="Exception">Sandbox instantiation exceptions.</exception>
    private StyleRule[] BuildSheetlet(Type sheetletType)
    {
        Type sheetletClosedType;
        try
        {
            // This supports both:
            // 1) "class ButtonSheetlet<T> : ISheetlet<T> where T : ..." (preferred), and
            // 2) "class ButtonSheetlet : ISheetlet<IButtonConfig"
            sheetletClosedType = sheetletType.ContainsGenericParameters
                ? sheetletType.MakeGenericType(GetType())
                : sheetletType;
        }
        catch (ArgumentException)
        {
            _sawmill.Error($"{this} does not satisfy the constraints for {sheetletType}.");
            return [];
        }

        return _sandboxHelper.CreateInstance(sheetletClosedType) is not ISheetlet sheetlet
            ? throw new Exception($"Failed to create instance of sheetlet type {sheetletClosedType}.")
            : sheetlet.GetRules(this, this);
    }


    /// <summary>
    /// Builds the stylesheet.
    /// </summary>
    /// <returns>Stylesheet constructed from all the sheetlets.</returns>
    public Stylesheet Build()
    {
        // Sorts sheetlets by how "close" their attribute types are to the specific factory, letting us create an ordering
        // so that you can make overriding sheetlets.
        var sheetletTypes = _reflectionManager.FindTypesWithAttribute<SheetletAttribute>()
            .Where(t =>
            {
                t.TryGetCustomAttribute<SheetletAttribute>(out var attr);
                return attr!.Definitions.Any(f => f.IsInstanceOfType(this));
            })
            .OrderByDescending(t =>
            {
                t.TryGetCustomAttribute<SheetletAttribute>(out var attr);
                return GetSheetletDistance(attr!);
            })
            .ThenBy(t => t.Name)
            .ToList();

        var rules = new List<StyleRule>();

        foreach (var sheetletType in sheetletTypes)
        {
            rules.AddRange(BuildSheetlet(sheetletType));
        }

        return new Stylesheet(rules.ToArray());
    }

    /// <summary>
    /// Gets the distance from the attribute's types and the factory type in the inheritance hierarchy.
    /// </summary>
    /// <param name="attribute">Sheetlet attribute to measure from.</param>
    /// <returns>Distance from that sheetlet attribute factory to the actual factory type.</returns>
    private int GetSheetletDistance(SheetletAttribute attribute)
    {
        var dist = 0;

        foreach (var type in GetType().GetClassHierarchy())
        {
            if (attribute.Definitions.Contains(type))
                return dist;

            dist++;
        }

        return int.MaxValue;
    }
}
