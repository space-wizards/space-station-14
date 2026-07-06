using Content.Shared.Movement.Systems;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Wieldable.Components;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// Handles <see cref="NyctophobiaComponent"/>
/// </summary>
public abstract partial class SharedNyctophobiaSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;

    [Dependency] private readonly SharedLightSensitiveSystem _lighting = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NyctophobiaComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<NyctophobiaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<NyctophobiaComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<NyctophobiaComponent, EntityLightUpdateEvent>(OnLightUpdate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NyctophobiaComponent>();

        while (query.MoveNext(out var uid, out var nyctophobia))
        {
            var entLightLevel = _lighting.GetEntityIllumination(uid, nyctophobia);

            SetDarkness(uid, nyctophobia, entLightLevel < nyctophobia.DarknessThreshold);

        }
    }

    protected virtual void SetDarkness(EntityUid uid, NyctophobiaComponent nyctophobia, bool inDarkness)
    {
        if (nyctophobia.InDarkness == inDarkness)
        {
            return;
        }

        nyctophobia.InDarkness = inDarkness;
        _speedModifier.RefreshMovementSpeedModifiers(uid);
        // if (nyctophobia.InDarkness == true)
        // {
        //     EnsureComp<AutoEmoteComponent>(uid);
        //     _autoEmote.AddEmote(uid, "Scream");
        // }

        Dirty(uid, nyctophobia);
    }

    private void OnInit(Entity<NyctophobiaComponent> ent, ref ComponentInit args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(ent);
    }

    private void OnShutdown(Entity<NyctophobiaComponent> ent, ref ComponentShutdown args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(ent);
    }

    // Handles movement speed for entities with impaired mobility.
    // Applies a speed penalty, but counteracts it if the entity is holding a non-wielded mobility aid.
    private void OnRefreshMovementSpeed(Entity<NyctophobiaComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!ent.Comp.InDarkness)
            return;

        args.ModifySpeed(ent.Comp.SpeedModifier);
    }

    private void OnLightUpdate(Entity<NyctophobiaComponent> ent, ref EntityLightUpdateEvent args)
    {
        _speedModifier.RefreshMovementSpeedModifiers(ent);
    }

}
