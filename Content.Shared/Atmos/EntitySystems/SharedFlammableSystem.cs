using Content.Shared.Atmos.Components;
using Content.Shared.Database;
using Content.Shared.IgnitionSource;
using Content.Shared.Toggleable;

namespace Content.Shared.Atmos.EntitySystems;

public abstract class SharedFlammableSystem : EntitySystem
{
    [Dependency] private readonly SharedIgnitionSourceSystem _ignitionSourceSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public void UpdateAppearance(EntityUid uid, FlammableComponent? flammable = null, AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref flammable, ref appearance))
            return;

        _appearance.SetData(uid, FireVisuals.OnFire, flammable.OnFire, appearance);
        _appearance.SetData(uid, FireVisuals.FireStacks, flammable.FireStacks, appearance);

        // Also enable toggleable-light visuals
        // This is intended so that matches & candles can re-use code for un-shaded layers on in-hand sprites.
        // However, this could cause conflicts if something is ACTUALLY both a toggleable light and flammable.
        // if that ever happens, then fire visuals will need to implement their own in-hand sprite management.
        _appearance.SetData(uid, ToggleableVisuals.Enabled, flammable.OnFire, appearance);
    }
    public void Ignite(EntityUid uid, EntityUid ignitionSource, FlammableComponent? flammable = null,
        EntityUid? ignitionSourceUser = null)
    {
        if (!Resolve(uid, ref flammable))
            return;

        if (flammable.AlwaysCombustible)
        {
            flammable.FireStacks = Math.Max(flammable.FirestacksOnIgnite, flammable.FireStacks);
        }

        if (flammable.FireStacks > 0 && !flammable.OnFire)
        {
            var extinguished = new IgnitedEvent();
            RaiseLocalEvent(uid, ref extinguished);
        }

        UpdateAppearance(uid, flammable);

    }
}
