using Robust.Server.Player;
using Content.Server.GameTicking;
using Content.Shared.Implants.Components;
using Content.Server.Mind;
using Content.Server.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Server.Popups;
using Robust.Shared.Containers;
using Content.Shared.IdentityManagement;

namespace Content.Server.Mindshield;
/// <summary>
/// For checking for Mindshield implant at start and giving them the component
/// </summary>

public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        //Commenting this stuff out as it isn't really needed unless Revs are active and I want to find a better solution anyway.
        //SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayerJobAssigned);
        //SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
    }
    //private void OnPlayerJobAssigned(RulePlayerJobsAssignedEvent ev)
    //{
    //    MindShieldCheck();
    //}
    //private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    //{
    //    MindShieldCheck();
    //}

    /// <summary>
    /// Checks for Mindshields at the start to give component and removes component from head revs for identification.
    /// </summary>

    public void MindShieldCheck()
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

