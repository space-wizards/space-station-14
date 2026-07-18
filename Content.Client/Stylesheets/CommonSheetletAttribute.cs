using JetBrains.Annotations;

namespace Content.Client.Stylesheets;

/// <summary>
///     Attribute used to mark a sheetlet class. Stylesheets can use this attribute to locate and load sheetlets.
/// </summary>
[PublicAPI]
[MeansImplicitUse]
public sealed class CommonSheetletAttribute : Attribute
{

}
