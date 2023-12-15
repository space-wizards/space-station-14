using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.Ninja.Systems;
using Content.Shared.Communications;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server.Communications;

public sealed class CommsHackerSystem : SharedCommsHackerSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    // TODO: remove when generic check event is used
    [Dependency] private readonly NinjaGlovesSystem _gloves = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CommsHackerComponent, BeforeInteractHandEvent>(OnBeforeInteractHand);
        SubscribeLocalEvent<CommsHackerComponent, TerrorDoAfterEvent>(OnDoAfter);
    }

    /// <summary>
    /// Start the doafter to hack a comms console
    /// </summary>
    private void OnBeforeInteractHand(EntityUid uid, CommsHackerComponent comp, BeforeInteractHandEvent args)
    {
        if (args.Handled || !HasComp<CommunicationsConsoleComponent>(args.Target))
            return;

        // TODO: generic check event
        if (!_gloves.AbilityCheck(uid, args, out var target))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, uid, comp.Delay, new TerrorDoAfterEvent(), target: target, used: uid, eventTarget: uid)
        {
            BreakOnDamage = true,
            BreakOnUserMove = true,
            MovementThreshold = 0.5f,
            CancelDuplicate = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    /// <summary>
    /// Call in a random threat and do cleanup.
    /// </summary>
    private void OnDoAfter(EntityUid uid, CommsHackerComponent comp, TerrorDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target == null)
            return;

        var threats = _proto.Index<WeightedRandomPrototype>(comp.Threats);
        var threat = threats.Pick(_random);
        CallInThreat(_proto.Index<NinjaHackingThreatPrototype>(threat));

        // prevent calling in multiple threats
        RemComp<CommsHackerComponent>(uid);

        var ev = new ThreatCalledInEvent(uid, args.Target.Value);
        RaiseLocalEvent(args.User, ref ev);
    }

    /// <summary>
    /// Makes announcement and adds game rule of the threat.
    /// </summary>
    public void CallInThreat(NinjaHackingThreatPrototype ninjaHackingThreat)
    {
        _gameTicker.StartGameRule(ninjaHackingThreat.Rule, out _);
        _chat.DispatchGlobalAnnouncement(Loc.GetString(ninjaHackingThreat.Announcement), playSound: true, colorOverride: Color.Red);
    }
}

/// <summary>
/// Raised on the user when a threat is called in on the communications console.
/// </summary>
/// <remarks>
/// If you add <see cref="CommsHackerComponent"/>, make sure to use this event to prevent adding it twice.
/// For example, you could add a marker component after a threat is called in then check if the user doesn't have that marker before adding CommsHackerComponent.
/// </remarks>
[ByRefEvent]
public record struct ThreatCalledInEvent(EntityUid Used, EntityUid Target);
