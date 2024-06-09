using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Scurret;

namespace Content.Server.Scurret;

/// <summary>
/// This handles some unique Scurret things, like backflipping when they are interacted with.
/// </summary>
public sealed class ScurretSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScurretComponent, InteractionAttemptFailed>(OnInteractFailed);
    }

    public void OnInteractFailed(EntityUid uid, ScurretComponent _, InteractionAttemptFailed args)
    {
        RaiseLocalEvent(uid, new BackflipActionEvent());
    }
}
