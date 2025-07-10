using Content.Shared.GameTicking;
using Content.Shared.Holiday;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Holiday;

/// <inheritdoc />
public sealed class HolidaySystem : SharedHolidaySystem
{
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<TickerLobbyStatusEvent>(EnterLobby); // TODO find a way to do this in shared

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
        if (!comp.Sprite.TryGetValue(data, out var rsiString) || args.Sprite == null)
            return;

        var path = SpriteSpecifierSerializer.TextureRoot / rsiString;
        if (_resCache.TryGetResource(path, out RSIResource? rsi))
            _sprite.SetBaseRsi((ent.Owner, args.Sprite), rsi.RSI);
    }
}
