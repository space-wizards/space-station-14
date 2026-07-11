using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

/// <summary>
/// A sheetlet is a class that takes in SheetletConfigs as type constraints and returns style rules that are collected
/// to form a stylesheet.
/// </summary>
/// <typeparam name="T">Sheetlet configs</typeparam>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface ISheetlet<in T> : ISheetlet
    where T : ISheetletConfig
{
    /// <summary>
    /// Generates the style rules for the sheetlet.
    /// </summary>
    /// <param name="factory">Style factory resolver</param>
    /// <param name="config">Sheetlet configs</param>
    /// <returns>Style rules</returns>
    StyleRule[] GetRules(StylesheetFactory factory, T config);

    /// <summary>
    /// Generates the style rules for the sheetlet.
    /// </summary>
    /// <param name="factory">Style factory resolver</param>
    /// <param name="config">Sheetlet configs</param>
    /// <returns>Style rules</returns>
    StyleRule[] ISheetlet.GetRules(StylesheetFactory factory, ISheetletConfig config)
    {
        return GetRules(factory, (T)config);
    }
}

/// <summary>
/// Non-generic ISheetlet for usage within reflection systems.
/// </summary>
public interface ISheetlet : ISheetletConfig
{
    /// <summary>
    /// Generates the style rules for the sheetlet.
    /// </summary>
    /// <param name="factory">Style factory resolver</param>
    /// <param name="config">Sheetlet configs</param>
    /// <returns>Style rules</returns>
    StyleRule[] GetRules(StylesheetFactory factory, ISheetletConfig config);
}
