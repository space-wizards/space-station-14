using System.Globalization;
using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.RichText;
using Robust.Shared.Configuration;

namespace Content.Client.Stylesheets.Fonts;

/// <summary>
/// Manages the default set of fonts for the game, and respects player preferences.
/// </summary>
public interface IFontSelectionManager : IFontAccessor
{
    /// <summary>
    /// Raised when font preferences have changed.
    /// </summary>
    event Action<StandardFontType>? OnFontChanged;

    /// <summary>
    /// Get the default font for a type, ignoring preferences.
    /// </summary>
    /// <remarks>
    /// This is intended specifically for settings menu use cases. Do not use anywhere else!
    /// </remarks>
    /// <param name="type">The type of the font to get.</param>
    /// <param name="scaledSize">The size, pre-scaled, to get.</param>
    Font GetDefaultFont(StandardFontType type, int scaledSize);

    /// <summary>
    /// Scale a font size.
    /// </summary>
    /// <remarks>
    /// This is intended specifically for settings menu use cases, and is not needed for typical access use.
    /// </remarks>
    int ScaleFontSize(float scale, int size);

    /// <summary>
    /// Initialize the <see cref="IFontSelectionManager"/>.
    /// </summary>
    void Initialize();
}

/// <summary>
/// Implementation of <see cref="IFontSelectionManager"/>.
/// </summary>
internal sealed partial class FontSelectionManager : IFontSelectionManager, IPostInjectInit
{
    [Dependency] private readonly IConfigurationManager _cfg = null!;
    [Dependency] private readonly ISystemFontManager _fontManager = null!;
    [Dependency] private readonly IResourceCache _resourceCache = null!;
    [Dependency] private readonly ILogManager _logManager = null!;
    [Dependency] private readonly FontTagHijackHolder _fontTagHijack = null!;

    private readonly Dictionary<StandardFontType, FontData> _fontData = [];
    private readonly Dictionary<string, ISystemFontFace[]> _systemFontFaces = [];

    private ISawmill _sawmill = null!;

    public event Action<StandardFontType>? OnFontChanged;

    public void Initialize()
    {
        _fontTagHijack.Hijack = HijackFontTag;

        var systemFonts = _fontManager.SystemFontFaces
            .GroupBy(face => face.GetLocalizedFamilyName(CultureInfo.InvariantCulture));

        foreach (var group in systemFonts)
        {
            _systemFontFaces.Add(group.Key, group.ToArray());
        }

        InitializeFont(
            StandardFontType.Main,
            DefaultFontMain,
            FontCVars.MainFamilyName,
            FontCVars.MainScale);

        InitializeFont(
            StandardFontType.Title,
            DefaultFontTitle,
            FontCVars.TitleFamilyName,
            FontCVars.TitleScale);

        InitializeFont(
            StandardFontType.MachineTitle,
            DefaultFontMachineTitle,
            FontCVars.MachineTitleFamilyName,
            FontCVars.MachineTitleScale);

        InitializeFont(
            StandardFontType.Monospace,
            DefaultFontMonospace,
            FontCVars.MonospaceFamilyName,
            FontCVars.MonospaceScale);

        return;

        void InitializeFont(
            StandardFontType fontType,
            FontFamilyStack stack,
            CVarDef<string> cVarFamilyName,
            CVarDef<float> cVarScale)
        {
            var fontData = new FontData
            {
                BaseStack = stack,
            };

            _fontData.Add(fontType, fontData);

            _cfg.OnValueChanged(cVarFamilyName,
                val =>
                {
                    if (string.IsNullOrWhiteSpace(val))
                    {
                        fontData.CustomFontFaces = null;
                    }
                    else
                    {
                        if (_systemFontFaces.TryGetValue(val, out var systemFaces))
                        {
                            fontData.CustomFontFaces = systemFaces;
                        }
                        else
                        {
                            _sawmill.Warning("Cannot find system font family {Family} for font {Font}", val, fontType);
                            fontData.CustomFontFaces = null;
                        }
                    }

                    FontChanged(fontType, fontData);
                },
                true);

            _cfg.OnValueChanged(cVarScale,
                val =>
                {
                    fontData.Scale = Math.Clamp(val, 0.75f, 3f);

                    FontChanged(fontType, fontData);
                },
                true);
        }
    }

    public Font GetDefaultFont(StandardFontType type, int scaledSize)
    {
        var data = _fontData[type];
        var fonts = GetDefaultFontFor(data, FontKind.Regular, scaledSize);

        return new StackedFont(fonts.ToArray());
    }

    public Font GetFont(StandardFontType type, int size, FontKind kind = FontKind.Regular)
    {
        var data = _fontData[type];

        if (!data.CachedFontInstances.TryGetValue((kind, size), out var font))
        {
            font = CreateFont(data, kind, size);

            data.CachedFontInstances.Add((kind, size), font);
        }

        return font;
    }

    private StackedFont CreateFont(FontData data, FontKind kind, int size)
    {
        var scaledSize = ScaleFontSize(data, size);

        var fonts = GetDefaultFontFor(data, kind, scaledSize);

        if (data.CustomFontFaces != null)
        {
            var face = FontSelectionHelpers.SelectClosest(
                data.CustomFontFaces,
                kind.GetWeight(),
                kind.GetSlant());

            fonts = fonts.Prepend(face.Load(scaledSize));
        }

        return new StackedFont(fonts.ToArray());
    }

    private IEnumerable<Font> GetDefaultFontFor(FontData data, FontKind kind, int scaledSize)
    {
        return data.BaseStack.GetFontPaths(kind)
            .Select(x => new VectorFont(_resourceCache.GetResource<FontResource>(x), scaledSize));
    }

    private void FontChanged(StandardFontType fontType, FontData data)
    {
        data.CachedFontInstances.Clear();

        _fontTagHijack.HijackUpdated();
        OnFontChanged?.Invoke(fontType);
    }

    void IPostInjectInit.PostInject()
    {
        _sawmill = _logManager.GetSawmill("style.font");
    }

    private int ScaleFontSize(FontData data, int size)
    {
        return ScaleFontSize(data.Scale, size);
    }

    public int ScaleFontSize(float scale, int size)
    {
        return (int)Math.Round(size * scale);
    }

    private sealed class FontData
    {
        public ISystemFontFace[]? CustomFontFaces;
        public float Scale;

        public required FontFamilyStack BaseStack;
        public readonly Dictionary<(FontKind, int), Font> CachedFontInstances = new();
    }
}
