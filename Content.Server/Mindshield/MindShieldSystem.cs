using Robust.Server.Player;
using System.Linq;
using Content.Server.GameTicking;
using Content.Server.Players;
using Content.Shared.Implants.Components;
using Content.Server.Mind;
using Robust.Shared.Log;
using Content.Server.Mindshield.Components;
using Content.Shared.Implants;

namespace Content.Server.Mindshield;
/// <summary>
/// For checking for Mindshield implant at start and giving them the component
/// </summary>
public sealed class MindShieldSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerSystem = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobAssigned);
        //SubscribeLocalEvent<ImplantEvent>(Implanted);
        //SubscribeLocalEvent<MindShieldComponent, ComponentInit>(OnMindShieldActivated);
    }
    private void OnJobAssigned(RulePlayerJobsAssignedEvent ev)
    {
        var players = ev.Players;
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
                        AddComp<MindShieldComponent>(mind.OwnedEntity.Value);
                    }
                }
            }
        }
    }
   
}
