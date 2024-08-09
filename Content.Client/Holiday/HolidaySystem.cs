using Content.Shared.Holiday;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Holiday;

public sealed class HolidaySystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _rescache = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<HolidayRsiSwapComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<HolidayRsiSwapComponent> ent, ref AppearanceChangeEvent args)
    {
        if (!_appearance.TryGetData<string>(ent, HolidayVisuals.Holiday, out var data, args.Component))
            return;

        var comp = ent.Comp;
        if (!comp.Sprite.TryGetValue(data, out var rsistring) || args.Sprite == null)
            return;

        var path = SpriteSpecifierSerializer.TextureRoot / rsistring;
        if (_rescache.TryGetResource(path, out RSIResource? rsi))
            args.Sprite.BaseRSI = rsi.RSI;
    }
}
