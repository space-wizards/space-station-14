using Content.Shared.GameTicking;
using Content.Shared.Holiday;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Holiday;

public sealed class HolidaySystem : SharedHolidaySystem
{
    [Dependency] private readonly IResourceCache _rescache = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<TickerLobbyStatusEvent>(EnterLobby);

        SubscribeLocalEvent<HolidayRsiSwapComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void EnterLobby(TickerLobbyStatusEvent _)
    {
        RefreshCurrentHolidays();
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
            _sprite.SetBaseRsi((ent.Owner, args.Sprite), rsi.RSI);
    }
}
