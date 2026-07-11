namespace Content.Client.Stylesheets;

/// <summary>
/// A style res
/// </summary>
public abstract partial class StylesheetFactory
{
    /// <summary>
    /// Name of the stylesheet that is generated.
    /// </summary>
    public abstract string StylesheetName { get; }

    public record NoConfig();

    private object _config;

    /// <remarks>
    ///     This constructor will not access any virtual or abstract properties, so you can set them from your config.
    /// </remarks>
    protected StylesheetFactory(object config)
    {
        IoCManager.InjectDependencies(this);
        _config = config;
        Stylesheet = default!;
    }
}
