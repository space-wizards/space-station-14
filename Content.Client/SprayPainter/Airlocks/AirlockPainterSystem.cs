using System.Linq;
using Content.Shared.SprayPainter.Airlocks;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.SprayPainter.Airlocks;

/// <summary>
/// This client portion handles caching airlock styles for the client.
/// </summary>
public sealed class AirlockPainterSystem : SharedAirlockPainterSystem
{
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly List<SprayPainterEntry> _entries = [];
    public IReadOnlyList<SprayPainterEntry> Entries => _entries;

    protected override void CacheStyles()
    {
        base.CacheStyles();

        _entries.Clear();
        foreach (var style in Styles)
        {
            var name = style.Name;
            var maybeIconPath = Groups
                .FindAll(group => group.StylePaths.ContainsKey(name))
                .MaxBy(proto => proto.IconPriority)
                ?.StylePaths[name];
            if (maybeIconPath is not {} iconPath)
            {
                _entries.Add(new SprayPainterEntry(name, null));
                continue;
            }

            var doorRsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / iconPath);
            if (!doorRsi.RSI.TryGetState("closed", out var icon))
            {
                _entries.Add(new SprayPainterEntry(name, null));
                continue;
            }

            _entries.Add(new SprayPainterEntry(name, icon.Frame0));
        }
    }
}

public record SprayPainterEntry(string Name, Texture? Icon);
