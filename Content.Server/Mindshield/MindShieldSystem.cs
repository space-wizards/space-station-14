using Robust.Server.Player;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Shared.Implants.Components;
using Content.Server.Mind;
using Robust.Shared.Log;
using Content.Server.Mindshield.Components;
using Content.Shared.Implants;
using Content.Shared.Revolutionary;
using Content.Server.Popups;
using Robust.Shared.Containers;
using System.Xml.Linq;
using Linguini.Bundle.Errors;
using Content.Server.GameTicking.Rules.Components;

namespace Content.Server.Mindshield;
/// <summary>
/// For checking for Mindshield implant at start and giving them the component
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _sharedContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobAssigned);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerJoin);
        //SubscribeLocalEvent<ImplantEvent>(Implanted);
        //SubscribeLocalEvent<MindShieldComponent, ComponentInit>(OnMindShieldActivated);
    }
    private void OnJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        ApplyMindShield();
    }
    private void OnPlayerJoin(PlayerSpawnCompleteEvent ev)
    {
        ApplyMindShield();
    }
    /// <summary>
    /// On round start and when players join, they will be given the Mindshield component to prevent conversion.
    /// </summary>
    private void ApplyMindShield()
    {
        var shield = AllEntityQuery<ImplantedComponent>();
        while (shield.MoveNext(out var uid, out var comp))
        {
            var mind = _mind.GetMind(uid);
            var implants = comp.ImplantContainer.ContainedEntities;
            foreach (var implant in implants)
            {
                if (mind != null)
                {
                    if (Name(implant) == "mind-shield implant" && mind.OwnedEntity != null)
                    {
                        EnsureComp<MindShieldComponent>(mind.OwnedEntity.Value);
                        var name = mind.CharacterName;
                        if (HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity.Value))
                        {
                            _popup.PopupEntity(Loc.GetString("head-rev-break-mindshield"), mind.OwnedEntity.Value);
                            _sharedContainer.TryRemoveFromContainer(implant, true);
                            break;
                        }
                        if (HasComp<RevolutionaryComponent>(mind.OwnedEntity.Value) && !HasComp<HeadRevolutionaryComponent>(mind.OwnedEntity.Value))
                        {
                            RemComp<RevolutionaryComponent>(mind.OwnedEntity.Value);
                            if (name != null)
                            {
                                _popup.PopupEntity(Loc.GetString("rev-break-control", ("name", name)), mind.OwnedEntity.Value);
                            }
                        }
                    }
                }
            }
        }
    }
}
