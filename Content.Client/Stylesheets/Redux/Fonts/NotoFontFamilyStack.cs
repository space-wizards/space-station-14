using JetBrains.Annotations;
using Robust.Client.ResourceManagement;

namespace Content.Client.Stylesheets.Redux.Fonts;

[PublicAPI]
public sealed class NotoFontFamilyStack(IResourceCache resCache, string variant = "") : FontFamilyStack(resCache)
{
    public override string FontPrimary => $"/Fonts/NotoSans{variant}/NotoSans{variant}-{{0}}.ttf";

    public override string FontSymbols => "/Fonts/NotoSans/NotoSansSymbols-{2}.ttf";

    public override string[] Extra => new[] { "/Fonts/NotoSans/NotoSansSymbols2-Regular.ttf" };
}
