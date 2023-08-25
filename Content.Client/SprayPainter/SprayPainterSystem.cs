using Content.Shared.SprayPainter;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using System.Linq;
using Robust.Shared.Graphics;

namespace Content.Client.SprayPainter;

public sealed class SprayPainterSystem : SharedSprayPainterSystem
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    public List<SprayPainterEntry> Entries { get; private set; } = new();

    public override void Initialize()
    {
        base.Initialize();

        foreach (string style in Styles)
        {
            string? iconPath = Groups
              .FindAll(x => x.StylePaths.ContainsKey(style))?
              .MaxBy(x => x.IconPriority)?.StylePaths[style];
            if (iconPath == null)
            {
                Entries.Add(new SprayPainterEntry(style, null));
                continue;
            }

            RSIResource doorRsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / new ResPath(iconPath));
            if (!doorRsi.RSI.TryGetState("closed", out var icon))
            {
                Entries.Add(new SprayPainterEntry(style, null));
                continue;
            }

            Entries.Add(new SprayPainterEntry(style, icon.Frame0));
        }
    }
}

public sealed class SprayPainterEntry
{
    public string Name;
    public Texture? Icon;

    public SprayPainterEntry(string name, Texture? icon)
    {
        Name = name;
        Icon = icon;
    }
}
