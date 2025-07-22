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

        SubscribeNetworkEvent<DoRefreshHolidaysEvent>(UpdateHolidays);

        SubscribeLocalEvent<HolidayRsiSwapComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    /// <summary>
    ///     Update's client holiday list.
    /// </summary>
    /// <param name="args">Sent by Server.HolidaySystem when changing holidays.</param>
    private void UpdateHolidays(DoRefreshHolidaysEvent args)
    {
        SetActiveHolidays(args.Date);
    }

    /// <summary>
    ///     Swaps the rsi of particularly festive entities during the holiday.
    /// </summary>
    private void OnAppearanceChange(Entity<HolidayRsiSwapComponent> ent, ref AppearanceChangeEvent args)
    {
        // Get the holiday enum
        if (!_appearance.TryGetData<string>(ent, HolidayVisuals.Holiday, out var data, args.Component))
        {
            // No holiday, so set to default
            if (ent.Comp.Default != null)
                SetRsi((ent.Owner, args.Sprite), ent.Comp.Default);

            return;
        }

        // Get the new rsi
        if (!ent.Comp.Sprite.TryGetValue(data, out var rsiString))
            return;

        SetRsi((ent.Owner, args.Sprite), rsiString);
    }

    /// <summary>
    ///     Helper method for <see cref="OnAppearanceChange"/>. Does the actual setting of the rsi.
    /// </summary>
    private void SetRsi(Entity<SpriteComponent?> ent, string newRsi)
    {
        var path = SpriteSpecifierSerializer.TextureRoot / newRsi;

        if (_resCache.TryGetResource(path, out RSIResource? rsi))
            _sprite.SetBaseRsi(ent, rsi.RSI);
    }
}
