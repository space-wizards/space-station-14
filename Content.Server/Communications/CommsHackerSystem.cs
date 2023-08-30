using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Ninja.Systems;
using Content.Shared.Communications;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Robust.Shared.Random;

namespace Content.Server.Communications;

public sealed class CommsHackerSystem : SharedCommsHackerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    // TODO: remove when generic check event is used
    [Dependency] private readonly NinjaGlovesSystem _gloves = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommsHackerComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<CommsHackerComponent, TerrorDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    private void OnInteractionAttempt(EntityUid uid, CommsHackerComponent comp, InteractionAttemptEvent args)
    {
        // TODO: generic check event
        if (!_gloves.AbilityCheck(uid, args, out var target))
            return;

        if (!HasComp<CommunicationsConsoleComponent>(target))
            return;

        var doAfterArgs = new DoAfterArgs(uid, comp.Delay, new TerrorDoAfterEvent(), target: target, used: uid, eventTarget: uid)
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
        if (args.Cancelled || args.Handled || comp.Threats.Count == 0 || args.Target == null)
            return;

        var threat = _random.Pick(comp.Threats);
        CallInThreat(threat);

        // prevent calling in multiple threats
        RemComp<CommsHackerComponent>(uid);

        var ev = new ThreatCalledInEvent(uid, args.Target.Value);
        RaiseLocalEvent(args.User, ref ev);
    }

    /// <summary>
    /// Makes announcement and adds game rule of the threat.
    /// </summary>
    public void CallInThreat(Threat threat)
    {
        _gameTicker.StartGameRule(threat.Rule, out _);
        _chat.DispatchGlobalAnnouncement(Loc.GetString(threat.Announcement), playSound: true, colorOverride: Color.Red);
    }
}

/// <summary>
/// DoAfter event for comms console terror ability.
/// </summary>
public sealed partial class TerrorDoAfterEvent : SimpleDoAfterEvent { }

/// <summary>
/// Raised on the user when a threat is called in on the communications console.
/// </summary>
/// <remarks>
/// If you add <see cref="CommsHackerComponent"/>, make sure to use this event to prevent adding it twice.
/// For example, you could add a marker component after a threat is called in then check if the user doesn't have that marker before adding CommsHackerComponent.
/// </remarks>
[ByRefEvent]
public record struct ThreatCalledInEvent(EntityUid Used, EntityUid Target);
