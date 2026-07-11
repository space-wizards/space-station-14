using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

/// <summary>
/// A sheetlet is a class that takes in SheetletConfigs as type constraints and returns style rules that are collected
/// to form a stylesheet.
/// </summary>
/// <typeparam name="T">Sheetlet configs</typeparam>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface ISheetlet<in T>
    where T : ISheetletConfig
{
    /// <summary>
    /// Generates the style rules for the sheetlet.
    /// </summary>
    /// <param name="resolver">Resolves stylesheet-dependent style properties (e.g. paths, textures, resources)</param>
    /// <param name="config">Sheetlet configs</param>
    /// <returns>Style rules</returns>
    StyleRule[] GetRules(StylesheetFactory resolver, T config);
}
