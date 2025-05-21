using System;
using Content.Shared.Popups;
using Robust.Shared.Physics;
using Robust.Shared.Map;

namespace Content.Shared.Limb;

public sealed class SharedLimbHealthSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    public LimbSlot? GetLimbFromCoords(EntityUid target, EntityCoordinates click)
    {
        var bounds = _physics.GetWorldAABB(target);
        var ratio = (click.Position.Y - bounds.Bottom) / bounds.Height;

        if (ratio >= 0.8f)
            return LimbSlot.Head;
        if (ratio >= 0.6f)
            return LimbSlot.Arms;
        if (ratio >= 0.4f)
            return LimbSlot.Torso;
        if (ratio >= 0.3f)
            return LimbSlot.Groin;
        return LimbSlot.Legs;
    }

    public void ShowLimbHealth(EntityUid target, EntityCoordinates click, EntityUid user)
    {
        if (!TryComp<LimbHealthComponent>(target, out var health))
            return;

        var limb = GetLimbFromCoords(target, click);
        if (limb == null)
            return;

        var value = health.Health.TryGetValue(limb.Value, out var v) ? v : 0;
        _popup.PopupClient($"{limb.Value} health: {value}", user, user);
    }

    public void DamageAtCoords(EntityUid target, int amount, EntityCoordinates click)
    {
        if (!TryComp<LimbHealthComponent>(target, out var health))
            return;

        var limb = GetLimbFromCoords(target, click);
        if (limb == null)
            return;

        if (!health.Health.ContainsKey(limb.Value))
            return;

        health.Health[limb.Value] = Math.Max(0, health.Health[limb.Value] - amount);
    }
}
