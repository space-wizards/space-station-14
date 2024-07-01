using Content.Server.Chat.Systems;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Standing;

namespace Content.Server._CustomStuff.Traits;

public sealed class LayEmoteSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _modifier = default!;
    [Dependency] private readonly StandingStateSystem _standingSystem = default!;
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LayEmoteComponent, ComponentShutdown>(OnShutdown);
        // SubscribeLocalEvent<LayEmoteComponent, BuckleChangeEvent>(OnBuckleChange);
        SubscribeLocalEvent<LayEmoteComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<LayEmoteComponent, EmoteEvent>(OnEmote);
        SubscribeLocalEvent<LayEmoteComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
    }

    // On shutdown, make sure people are standing and reset movement speed
    private void OnShutdown(EntityUid uid, LayEmoteComponent component, ComponentShutdown args)
    {
        _standingSystem.Stand(uid);
        _bodySystem.UpdateMovementSpeed(uid);
    }

    // If buckled, make sure someone is standing. Unbuckling while laying down should keep someone laying down and vice versa.
    /*
    private void OnBuckleChange(EntityUid uid, LayEmoteComponent component, ref BuckleChangeEvent args)
    {
        if (args.Buckling && component.Laying)
            _standingSystem.Stand(args.BuckledEntity);

        if (!args.Buckling && component.Laying)
            _standingSystem.Down(args.BuckledEntity);
    }
    */ // Removing this part of the code because buckles were changed, same with the above comment

    private void OnMobStateChanged(EntityUid uid, LayEmoteComponent component, MobStateChangedEvent args)
    {
        // Hoping this should work fine as going crit - dead or dead - crit shouldn't matter, and crit - alive would stand you up anyways.
        component.Laying = false;
    }

    // Checks every player emote.
    private void OnEmote(EntityUid uid, LayEmoteComponent component, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(uid))
            return;

        // If they're not laying down & they emote to lay down, make them.
        if (!component.Laying && args.Emote.ID == component.LayEmoteId)
        {
            component.Laying = true;
            _standingSystem.Down(uid);
            _modifier.RefreshMovementSpeedModifiers(uid);
        }
        else if (component.Laying && args.Emote.ID == component.StandEmoteId) // If they are laying down and want to stand, reset their movement speed.
        {
            component.Laying = false;
            _standingSystem.Stand(uid);
            _modifier.RefreshMovementSpeedModifiers(uid);
        }
    }

    // Sets their movement speed to 0 if they're laying down.
    private void OnRefreshMovementSpeedModifiers(EntityUid uid, LayEmoteComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (component.Laying)
            args.ModifySpeed(0f, 0f);

        if (!component.Laying)
            args.ModifySpeed(1f, 1f);
    }
}
