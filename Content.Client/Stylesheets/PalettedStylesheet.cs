using JetBrains.Annotations;

namespace Content.Client.Stylesheets;

/// <summary>
///     The base class for all stylesheets, providing core functionality and helpers.
/// </summary>
[PublicAPI]
public abstract partial class PalettedStylesheet : BaseStylesheet
{
    protected PalettedStylesheet(object config) : base(config)
    {
    }
}
