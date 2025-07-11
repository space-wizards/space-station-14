using Content.Shared.Holiday;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.Holiday;

/// <inheritdoc />
public sealed class HolidaySystem : SharedHolidaySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<HolidaysRefreshedEvent>(RefreshHolidays);

        SubscribeLocalEvent<HolidayRsiSwapComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    /// <summary>
    ///     Update's client holiday list.
    /// </summary>
    /// <param name="args">Sent by server HolidaySystem when changing holidays.</param>
    private void RefreshHolidays(HolidaysRefreshedEvent args)
    {
        CurrentHolidays.Clear();
        
        // Festively find what holidays we're celebrating
        foreach (var holiday in _prototypeManager.EnumeratePrototypes<HolidayPrototype>())
        {
            if (holiday.ShouldCelebrate(args.Now))
            {
                CurrentHolidays.Add(holiday);
            }
        }
    }

    /// <summary>
    ///     Swaps the rsi of particularly festive entities during the holiday.
    /// </summary>
    private void OnAppearanceChange(Entity<HolidayRsiSwapComponent> ent, ref AppearanceChangeEvent args)
    {
        // Get the holiday enum
        if (!_appearance.TryGetData<string>(ent, HolidayVisuals.Holiday, out var data, args.Component))
            return;

        // Get the new rsi
        if (args.Sprite == null || !ent.Comp.Sprite.TryGetValue(data, out var rsiString))
            return;
        var path = SpriteSpecifierSerializer.TextureRoot / rsiString;

        // Set the new rsi
        if (_resCache.TryGetResource(path, out RSIResource? rsi))
            _sprite.SetBaseRsi((ent.Owner, args.Sprite), rsi.RSI);
    }
}
