using Content.Shared.OmniPainter;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;
using System.Linq;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.OmniPainter
{
    public sealed class OmniPainterSystem : SharedOmniPainterSystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public List<OmniPainterEntry> Entries { get; private set; } = new();

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
                    Entries.Add(new OmniPainterEntry(style, null));
                    continue;
                }

                RSIResource doorRsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / new ResPath(iconPath));
                if (!doorRsi.RSI.TryGetState("closed", out var icon))
                {
                    Entries.Add(new OmniPainterEntry(style, null));
                    continue;
                }

                Entries.Add(new OmniPainterEntry(style, icon.Frame0));
            }
        }
    }

    public sealed class OmniPainterEntry
    {
        public string Name;
        public Texture? Icon;

        public OmniPainterEntry(string name, Texture? icon)
        {
            Name = name;
            Icon = icon;
        }
    }
}
