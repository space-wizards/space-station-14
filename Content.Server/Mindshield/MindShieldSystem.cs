using Robust.Server.Player;
using Content.Server.GameTicking;
using Content.Shared.Implants.Components;
using Content.Server.Mind;
using Content.Server.Mindshield.Components;
using Content.Shared.Revolutionary;
using Content.Server.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Shared.IdentityManagement;
using Content.Server.Chemistry.ReagentEffectConditions;
using YamlDotNet.Core.Tokens;

namespace Content.Server.Mindshield;
/// <summary>
/// For checking for Mindshield implant at start and giving them the component
/// </summary>

public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;

    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        base.Initialize();
        _sawmill = Logger.GetSawmill("preset");
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<MindShieldComponent, ComponentInit>(OnMindShield);
    }

    //Ghetto solution until I find a way to add a component when implanting.
    //I actually hate this so much

    private void OnMindShield(EntityUid uid, MindShieldComponent comp, ComponentInit componentInit)
    {
        OnImplanted();
    }
    private void OnPlayerJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        OnImplanted();
    }
    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        OnImplanted();
    }

    /// <summary>
    /// Checks for Mindshields at the start to give component and removes component from head revs for identification.
    /// </summary>

    public void OnImplanted()
    {
        var query = AllEntityQuery<ImplantedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var mind = _mind.GetMind(uid);
            var implants = comp.ImplantContainer.ContainedEntities;
            if (HasComp<MindShieldComponent>(uid) && !HasComp<RevolutionaryComponent>(uid))
                return;
            foreach (var implant in implants)
            {
                _sawmill.Error("Implants?");
                if (mind != null)
                {
                    if (HasComp<MindShieldComponent>(implant) && mind.OwnedEntity != null)
                    {
                        EnsureComp<MindShieldComponent>(mind.OwnedEntity.Value);
                        var name = Identity.Entity(uid, EntityManager);
                        if (mind.OwnedEntity != null)
                        {
                            if (HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity.Value))
                            {
                                _popup.PopupEntity(Loc.GetString("head-rev-break-mindshield"), mind.OwnedEntity.Value);
                                RemComp<MindShieldComponent>(mind.OwnedEntity.Value);
                                _sharedContainer.TryRemoveFromContainer(implant, true);
                                break;
                            }
                            if (HasComp<RevolutionaryComponent>(mind.OwnedEntity.Value) && !HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity.Value))
                            {
                                RemComp<RevolutionaryComponent>(mind.OwnedEntity.Value);
                                _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), mind.OwnedEntity.Value);

                            }
                        }
                    }
                }
            }
        }
    }
}

