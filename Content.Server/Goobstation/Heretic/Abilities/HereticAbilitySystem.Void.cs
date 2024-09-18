using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Heretic.Components;
using Content.Server.Temperature.Components;
using Content.Shared.Damage;
using Content.Shared.Heretic;
using Content.Shared.Temperature.Components;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Components;

namespace Content.Server.Heretic.Abilities;

public sealed partial class HereticAbilitySystem : EntitySystem
{
    private void SubscribeVoid()
    {
        SubscribeLocalEvent<HereticComponent, HereticAristocratWayEvent>(OnAristocratWay);
        SubscribeLocalEvent<HereticComponent, HereticAscensionVoidEvent>(OnAscensionVoid);

        SubscribeLocalEvent<HereticComponent, HereticVoidBlastEvent>(OnVoidBlast);
        SubscribeLocalEvent<HereticComponent, HereticVoidBlinkEvent>(OnVoidBlink);
        SubscribeLocalEvent<HereticComponent, HereticVoidPullEvent>(OnVoidPull);
    }

    private void OnAristocratWay(Entity<HereticComponent> ent, ref HereticAristocratWayEvent args)
    {
        RemComp<TemperatureComponent>(ent);
        RemComp<TemperatureSpeedComponent>(ent);
        RemComp<RespiratorComponent>(ent);
    }
    private void OnAscensionVoid(Entity<HereticComponent> ent, ref HereticAscensionVoidEvent args)
    {
        RemComp<BarotraumaComponent>(ent);
        EnsureComp<AristocratComponent>(ent);
    }

    private void OnVoidBlast(Entity<HereticComponent> ent, ref HereticVoidBlastEvent args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var rod = Spawn("ImmovableVoidRod", Transform(ent).Coordinates);

        if (TryComp(rod, out PhysicsComponent? phys))
        {
            _phys.SetLinearDamping(rod, phys, 0f);
            _phys.SetFriction(rod, phys, 0f);
            _phys.SetBodyStatus(rod, phys, BodyStatus.InAir);

            var xform = Transform(rod);
            var vel = Transform(ent).WorldRotation.ToWorldVec() * 15f;

            _phys.SetLinearVelocity(rod, vel, body: phys);
            xform.LocalRotation = Transform(ent).LocalRotation;
        }

        args.Handled = true;
    }

    private void OnVoidBlink(Entity<HereticComponent> ent, ref HereticVoidBlinkEvent args)
    {
        if (!TryUseAbility(ent, args))
            return;

        _aud.PlayPvs(new SoundPathSpecifier("/Audio/Effects/tesla_consume.ogg"), ent);

        foreach (var pookie in GetNearbyPeople(ent, 2f))
            _stun.TryKnockdown(pookie, TimeSpan.FromSeconds(2.5f), true);

        _transform.SetCoordinates(ent, args.Target);

        // repeating for both sides
        _aud.PlayPvs(new SoundPathSpecifier("/Audio/Effects/tesla_consume.ogg"), ent);

        foreach (var pookie in GetNearbyPeople(ent, 2f))
            _stun.TryKnockdown(pookie, TimeSpan.FromSeconds(2.5f), true);

        args.Handled = true;
    }

    private void OnVoidPull(Entity<HereticComponent> ent, ref HereticVoidPullEvent args)
    {
        if (!TryUseAbility(ent, args))
            return;

        var topPriority = GetNearbyPeople(ent, 1.5f);
        var midPriority = GetNearbyPeople(ent, 2.5f);
        var farPriority = GetNearbyPeople(ent, 5f);

        // damage closest ones
        foreach (var pookie in topPriority)
            if (TryComp<DamageableComponent>(pookie, out var dmg))
                _dmg.SetAllDamage(pookie, dmg, dmg.TotalDamage + .5f);

        // stun close-mid range
        foreach (var pookie in midPriority)
            _stun.TryKnockdown(pookie, TimeSpan.FromSeconds(2.5f), true);

        // pull in farthest ones
        foreach (var pookie in farPriority)
            _throw.TryThrow(pookie, Transform(ent).Coordinates);

        args.Handled = true;
    }
}
