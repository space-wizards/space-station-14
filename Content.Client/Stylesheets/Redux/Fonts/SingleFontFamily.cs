using JetBrains.Annotations;
using Robust.Client.ResourceManagement;

namespace Content.Client.Stylesheets.Redux.Fonts;


[PublicAPI]
public sealed class SingleFontFamily(IResourceCache resCache, string singularFont) : FontFamilyStack(resCache)
{
    public override string FontPrimary => throw new NotImplementedException();

    public override string FontSymbols => throw new NotImplementedException();

    public override string FontFallback => throw new NotImplementedException();

    public override string[] Extra => throw new NotImplementedException();

    public override HashSet<FontKind> AvailableKinds => new() {FontKind.Regular};

    public string SingularFont = singularFont;

    protected override string[] GetFontPaths(FontKind kind)
    {
        return new[] {SingularFont};
    }
}
