using Content.Shared.AirlockPainter;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Utility;
using System.Linq;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.AirlockPainter
{
    public sealed class AirlockPainterSystem : SharedAirlockPainterSystem
    {
        [Dependency] private readonly IResourceCache _resourceCache = default!;

        public List<AirlockPainterEntry> Entries { get; private set; } = new();

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
                    Entries.Add(new AirlockPainterEntry(style, null));
                    continue;
                }

                RSIResource doorRsi = _resourceCache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / new ResPath(iconPath));
                if (!doorRsi.RSI.TryGetState("closed", out var icon))
                {
                    Entries.Add(new AirlockPainterEntry(style, null));
                    continue;
                }

                Entries.Add(new AirlockPainterEntry(style, icon.Frame0));
            }
        }
    }

    public sealed class AirlockPainterEntry
    {
        public string Name;
        public Texture? Icon;

        public AirlockPainterEntry(string name, Texture? icon)
        {
            Name = name;
            Icon = icon;
        }
    }
}
