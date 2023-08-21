using Content.Shared.Communications;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Serialization;

namespace Content.Server.Communications;

public sealed class CommsHackerSystem : SharedCommsHackerSystem
{
    // TODO: remove when generic check event is used
    [Dependency] private readonly SharedNinjaGlovesSystem _gloves = default!;

    public override void Intialize()
    {
        base.Intialize();

        SubscribeLocalEvent<NinjaTerrorComponent, InteractionAttemptEvent>(OnTerror);
        SubscribeLocalEvent<NinjaTerrorComponent, TerrorDoAfterEvent>(OnTerrorDoAfter);
    }

    /// <summary>
    private void OnInteractionAttempt(EntityUid uid, CommsHackerComponent comp, InteractionAttemptEvent args)
    {
        // TODO: generic check event
        if (!_gloves.GloveCheck(uid, args, out var gloves, out var user, out var target)

        if (!HasComp<CommunicationsConsoleComponent>(target))
            return;

        var doAfterArgs = new DoAfterArgs(args.User, comp.Delay, new TerrorDoAfterEvent(), target: target, used: uid, eventTarget: uid)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Cancel();
    }

    /// <summary>
    /// Call in a random threat and do cleanup.
    /// </summary>
    private void OnDoAfter(EntityUid uid, CommsHackerComponent comp, TerrorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || comp.Threats.Count == 0)
            return;

        var threat = _random.Pick(comp.Threats);
        CallInThreat(threat);

        // prevent calling in multiple threats
        RemComp<CommsHackerComponent>(uid);

        var ev = new ThreatCalledInEvent(uid, args.Target);
        RaiseLocalEvent(args.User, ref ev);
    }
}

/// <summary>
/// DoAfter event for comms console terror ability.
/// </summary>
[Serializable, NetSerializable]
public sealed class TerrorDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Raised on the user when a threat is called in on the communications console.
/// </summary>
/// <remarks>
/// If you add <see cref="CommsHackerComponent"/>, make sure to use this event to prevent adding it twice.
/// For example, you could add a marker component after a threat is called in then check if the user doesn't have that marker before adding CommsHackerComponent.
/// </remarks>
[ByRefEvent]
public record struct ThreatCalledEvent(EntityUid Used, EntityUid Target);
