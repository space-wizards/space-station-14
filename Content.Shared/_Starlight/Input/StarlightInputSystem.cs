using Content.Shared.Movement.Components;
using Content.Shared.NPC;
using Content.Shared.Starlight.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared._Starlight.Input;

// Taken from https://github.com/RMC-14/RMC-14
public sealed class StarlightInputSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _activeInputMoverEnabled;

    private EntityQuery<ActiveNPCComponent> _activeNpcQuery;
    private EntityQuery<ActorComponent> _actorQuery;
    private EntityQuery<MovementRelayTargetComponent> _movementRelayTargetQuery;

    public override void Initialize()
    {
        _activeNpcQuery = GetEntityQuery<ActiveNPCComponent>();
        _actorQuery = GetEntityQuery<ActorComponent>();
        _movementRelayTargetQuery = GetEntityQuery<MovementRelayTargetComponent>();

        SubscribeLocalEvent<ActiveInputMoverComponent, MapInitEvent>(OnActiveChanged);
        SubscribeLocalEvent<ActiveInputMoverComponent, PlayerAttachedEvent>(OnActiveChanged);
        SubscribeLocalEvent<ActiveInputMoverComponent, PlayerDetachedEvent>(OnActiveChanged);

        SubscribeLocalEvent<ActiveNPCComponent, MapInitEvent>(OnActiveChanged);
        SubscribeLocalEvent<ActiveNPCComponent, ComponentRemove>(OnActiveChanged);

        SubscribeLocalEvent<MovementRelayTargetComponent, ComponentRemove>(OnActiveChanged);
        SubscribeLocalEvent<MovementRelayTargetComponent, MapInitEvent>(OnActiveChanged);

        Subs.CVar(_config, StarlightCCVars.PhysicsActiveInputMoverEnabled, v => _activeInputMoverEnabled = v, true);
    }

    private void OnActiveChanged<TComp, TEvent>(Entity<TComp> ent, ref TEvent args) where TComp : IComponent?
    {
        if (!_activeInputMoverEnabled)
            return;

        if (_timing.ApplyingState || TerminatingOrDeleted(ent))
            return;

        if (ShouldBeActive(ent))
            EnsureComp<InputMoverComponent>(ent);
        else
            RemCompDeferred<InputMoverComponent>(ent);
    }

    private bool ShouldBeActive(EntityUid ent)
    {
        return _actorQuery.HasComp(ent) ||
               _activeNpcQuery.HasComp(ent) ||
               _movementRelayTargetQuery.CompOrNull(ent)?.Source != null;
    }
}
