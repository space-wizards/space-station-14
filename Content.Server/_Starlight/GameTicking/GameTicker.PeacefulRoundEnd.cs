using Content.Server.GameTicking;
using Content.Shared.Chemistry.Components;
using Content.Shared.Starlight.CCVar;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Movement.Components;
using Content.Shared.Mech.Components;
using Robust.Shared.Configuration;
using Content.Shared.GameTicking;
using Content.Shared.Starlight;

namespace Content.Server.Starlight.GameTicking;

public sealed class PeacefulRoundEndSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ISharedPlayersRoleManager _sharedPlayersRoleManager = default!;
    private bool _isEnabled = false;
    private bool _roundedEnded = false;
    private readonly List<PlayerFlags> _bypassFlags =
    [
        PlayerFlags.Mentor,
        PlayerFlags.Staff,
        PlayerFlags.ExtRoles
    ]; // I would love to make this a CVar. but it is just not in the cards.
    
    
    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(StarlightCCVars.PeacefulRoundEnd, v => _isEnabled = v, true);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnded);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<GotRehydratedEvent>(OnRehydrateEvent);
    }
    
    private void SpreadPeace(EntityUid target)
    {
        if (!_isEnabled || !_roundedEnded) return;
        if (_sharedPlayersRoleManager.HasAnyPlayerFlags(target, _bypassFlags)) return;
        EnsureComp<PacifiedComponent>(target);
    }
    
    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        SpreadPeace(ev.Mob);
    }

    private void OnRehydrateEvent(ref GotRehydratedEvent ev)
    {
        SpreadPeace(ev.Target);
    }

    private void OnRoundEnded(RoundEndTextAppendEvent ev)
    {
        _roundedEnded = true;
        foreach (var mob in EntityQuery<MobMoverComponent>())
        {
            SpreadPeace(mob.Owner);   
        }
        foreach (var mob in EntityQuery<MechComponent>())
        {
            SpreadPeace(mob.Owner);
        }
    }
}
