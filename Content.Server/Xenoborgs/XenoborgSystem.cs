using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Silicons.Borgs;
using Content.Shared.Destructible;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.Xenoborgs;

public sealed partial class XenoborgSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly BorgSystem _borg = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly XenoborgsRuleSystem _xenoborgsRule = default!;

    private static readonly Color XENOBORG_BRIEFING_COLOR = Color.BlueViolet;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MothershipCoreComponent, DestructionEventArgs>(OnDestroyed);
        SubscribeLocalEvent<XenoborgComponent, MindAddedMessage>(OnXenoborgMindAdded);
        SubscribeLocalEvent<XenoborgComponent, MindRemovedMessage>(OnXenoborgMindRemoved);
        SubscribeLocalEvent<MothershipCoreComponent, MindAddedMessage>(OnXenoborgCoreMindAdded);
        SubscribeLocalEvent<MothershipCoreComponent, MindRemovedMessage>(OnXenoborgCoreMindRemoved);
    }

    private void OnDestroyed(EntityUid ent, MothershipCoreComponent component, DestructionEventArgs args)
    {
        // if a mothership core is destroyed, it will see if there are any others
        var mothershipCoreQuery = AllEntityQuery<MothershipCoreComponent>();
        while (mothershipCoreQuery.MoveNext(out var mothershipCoreEnt, out _))
        {
            // if it finds a mothership core that is different from the one just destroyed,
            // it doesn't explode the xenoborgs
            if (mothershipCoreEnt != ent)
                return;
        }

        var xenoborgsRuleQuery = AllEntityQuery<XenoborgsRuleComponent>();
        if (xenoborgsRuleQuery.MoveNext(out _, out _))
            _xenoborgsRule.SendMothershipDeathAnnouncement();

        // explode all xenoborgs
        var xenoborgQuery = AllEntityQuery<XenoborgComponent, BorgTransponderComponent>();
        while (xenoborgQuery.MoveNext(out var xenoborgEnt, out _, out _))
        {
            // I got tired to trying to make this work via the device network.
            // so brute force it is...
            _borg.Destroy(xenoborgEnt);
        }
    }

    private void OnXenoborgMindAdded(EntityUid ent, XenoborgComponent comp, MindAddedMessage args)
    {
        if (_roles.MindHasRole<XenoborgRoleComponent>(args.Mind))
            return;

        _roles.MindAddRole(args.Mind, "MindRoleXenoborg", silent: true);

        if (!TryComp<ActorComponent>(ent, out var actorComp))
            return;

        _antag.SendBriefing(actorComp.PlayerSession,
            Loc.GetString("xenoborgs-welcome"),
            XENOBORG_BRIEFING_COLOR,
            new SoundPathSpecifier("/Audio/Ambience/Antag/xenoborg_start.ogg")
        );
    }

    private void OnXenoborgCoreMindAdded(EntityUid ent, MothershipCoreComponent comp, MindAddedMessage args)
    {
        if (_roles.MindHasRole<XenoborgCoreRoleComponent>(args.Mind))
            return;

        _roles.MindAddRole(args.Mind, "MindRoleMothershipCore", silent: true);

        if (!TryComp<ActorComponent>(ent, out var actorComp))
            return;

        _antag.SendBriefing(actorComp.PlayerSession,
            Loc.GetString("mothership-welcome"),
            XENOBORG_BRIEFING_COLOR,
            new SoundPathSpecifier("/Audio/Ambience/Antag/xenoborg_start.ogg")
        );
    }

    private void OnXenoborgMindRemoved(EntityUid ent, XenoborgComponent comp, MindRemovedMessage args)
    {
        EntityUid mind = args.Mind;

        if (_roles.MindHasRole<XenoborgRoleComponent>(mind))
            _roles.MindRemoveRole<XenoborgRoleComponent>(mind);
    }

    private void OnXenoborgCoreMindRemoved(EntityUid ent, MothershipCoreComponent comp, MindRemovedMessage args)
    {
        EntityUid mind = args.Mind;

        if (_roles.MindHasRole<XenoborgCoreRoleComponent>(mind))
            _roles.MindRemoveRole<XenoborgCoreRoleComponent>(mind);
    }
}
