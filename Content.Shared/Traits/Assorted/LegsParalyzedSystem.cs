using Content.Shared.Buckle.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;

namespace Content.Shared.Traits.Assorted;

public sealed partial class LegsParalyzedSystem : EntitySystem
{
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifierSystem = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private StandingStateSystem _standing = default!;


    [SubscribeLocalEvent]
    private void OnStartup(Entity<LegsParalyzedComponent> ent, ref ComponentStartup args)
    {
        // TODO: In future probably must be surgery related wound
        _movementSpeedModifierSystem.ChangeBaseSpeed(ent.Owner, ent.Comp.BaseWalkSpeed, ent.Comp.BaseSprintSpeed, 20);
    }

    ///<summary>
    ///Interject for Standup attempts and instead cancel them. Buckling is probably the only way they should be able to stand up.
    ///Unfortunately, there is no good options as far as trying to block the standing up and sitting down due to ForceStanding and several other parts not being built to be a cancelable event.
    ///so we have to head it off at the source.
    ///
    ///<see cref="WormSystem"/> for another implementation that does half of this.
    ///</summary>
    ///
    [SubscribeLocalEvent]
    private void OnStandEvent(Entity<LegsParalyzedComponent> ent, ref StandUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.Cancelled = true;
        args.Message = (Loc.GetString("legs-paralyzed-component-stand-attempt"), PopupType.SmallCaution);
        args.Autostand = false;
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<LegsParalyzedComponent> ent, ref ComponentShutdown args)
    {
        _stun.TryStanding(ent.Owner);
    }

    /// <summary>
    /// Because we've Interjected on all standup events, we need to manage the Standing up.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBuckled(Entity<LegsParalyzedComponent> ent, ref BuckledEvent args)
    {
        // Sit the player up forcibly, there is no good way to do this while _also_ shutting down standAttempt. because it's not built off of events.
        RemComp<KnockedDownComponent>(ent.Owner);
        _standing.Stand(ent.Owner, force: true);
    }

    [SubscribeLocalEvent]
    private void OnUnbuckled(Entity<LegsParalyzedComponent> ent, ref UnbuckledEvent args)
    {
        _stun.TryCrawling(ent.Owner,false,false, ent.Comp.DropOnUnbuckle, true);
    }

    [SubscribeLocalEvent]
    private void OnThrowPushbackAttempt(Entity<LegsParalyzedComponent> ent, ref ThrowPushbackAttemptEvent args)
    {
        args.Cancel();
    }
}
