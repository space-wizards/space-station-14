using System.Linq;
using Content.Server.GameTicking;
using Content.Server.NPC.Components;
using Content.Shared.Roles;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Prototypes;

namespace Content.Server.Traits.Assorted;

public sealed class PsychosisGainSystem : SharedPsychosisGainSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<Stats>(Stats);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(Spawned);
    }
    private void Spawned(PlayerSpawnCompleteEvent ev)
    {
        if (ev.JobId == null || !_prototypeManager.TryIndex<JobPrototype>(ev.JobId, out var job))
            return;

        var uid = ev.Player.AttachedEntity;
        if (uid == null)
            return;
        if (!TryComp<PsychosisGainComponent>(uid.Value, out var component))
            return;
        component.Resist = job.PsychosisGainResist;
        var evd = new Stats(false, component.Resist, component.Status, GetNetEntity(uid.Value));
        RaiseNetworkEvent(evd);
    }
    private void Stats(Stats psychosi, EntitySessionEventArgs args)
    {
        if (!TryComp<PsychosisGainComponent>(GetEntity(psychosi.PsychosisGain), out var psych))
            return;
        if (TryComp<NpcFactionMemberComponent>(GetEntity(psychosi.PsychosisGain), out var faction))
        {
            foreach (var fact in faction.Factions)
            {
                if (fact == "Syndicate" || fact == "Zombie")
                    return;
            }
        }
        psych.Status = psychosi.Status;
        if (TryComp<PsychosisComponent>(GetEntity(psychosi.PsychosisGain), out var psychosis))
            return;
        if (psychosi.Gained)
        {
            AddComp<PsychosisComponent>(GetEntity(psychosi.PsychosisGain));
        }
    }

}
