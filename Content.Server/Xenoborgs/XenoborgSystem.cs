using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Silicons.Borgs;
using Content.Shared.Destructible;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Xenoborgs.Components;
using Robust.Shared.Player;

namespace Content.Server.Xenoborgs;

public sealed partial class XenoborgSystem : EntitySystem
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly BorgSystem _borg = default!;
    [Dependency] private readonly SharedRoleSystem _roles = default!;
    [Dependency] private readonly XenoborgsRuleSystem _xenoborgsRule = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MothershipCoreComponent, DestructionEventArgs>(OnDestroyed);
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

    public void EnsureXenoborgRole(EntityUid mindId, EntityUid ent, XenoborgComponent xenoborgComp)
    {
        if (!_roles.MindHasRole<XenoborgRoleComponent>(mindId))
        {
            _roles.MindAddRole(mindId, "MindRoleXenoborg", silent: true);

            if (TryComp<ActorComponent>(ent, out var actorComp))
            {
                _antag.SendBriefing(actorComp.PlayerSession,
                    Loc.GetString("xenoborgs-welcome"),
                    Color.BlueViolet,
                    xenoborgComp.GreetSoundNotification);
            }
        }
    }

    public void RemoveXenoborgRole(EntityUid mindId)
    {
        if (_roles.MindHasRole<XenoborgRoleComponent>(mindId))
            _roles.MindRemoveRole<XenoborgRoleComponent>(mindId);
    }

    public void EnsureXenoborgCoreRole(EntityUid mindId, EntityUid ent, MothershipCoreComponent mothershipComp)
    {
        if (!_roles.MindHasRole<XenoborgCoreRoleComponent>(mindId))
        {
            _roles.MindAddRole(mindId, "MindRoleMothershipCore", silent: true);

            if (TryComp<ActorComponent>(ent, out var actorComp))
            {
                _antag.SendBriefing(actorComp.PlayerSession,
                    Loc.GetString("xenoborgs-welcome"),
                    Color.BlueViolet,
                    mothershipComp.GreetSoundNotification);
            }
        }
    }

    public void RemoveXenoborgCoreRole(EntityUid mindId)
    {
        if (_roles.MindHasRole<XenoborgCoreRoleComponent>(mindId))
            _roles.MindRemoveRole<XenoborgCoreRoleComponent>(mindId);
    }
}
