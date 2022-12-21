using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Gravity;

public abstract partial class SharedGravitySystem
{
    protected const float GravityKick = 100.0f;
    protected const float ShakeCooldown = 0.2f;

    private void InitializeShake()
    {
        SubscribeLocalEvent<GravityShakeComponent, EntityUnpausedEvent>(OnShakeUnpaused);
        SubscribeLocalEvent<GravityShakeComponent, ComponentGetState>(OnShakeGetState);
        SubscribeLocalEvent<GravityShakeComponent, ComponentHandleState>(OnShakeHandleState);
    }

    private void OnShakeUnpaused(EntityUid uid, GravityShakeComponent component, ref EntityUnpausedEvent args)
    {
        component.NextShake += args.PausedTime;
    }

    private void UpdateShake()
    {
        var curTime = Timing.CurTime;
        var gravityQuery = GetEntityQuery<GravityComponent>();

        foreach (var comp in EntityQuery<GravityShakeComponent>())
        {
            if (comp.NextShake <= curTime)
            {
                if (comp.ShakeTimes == 0 || !gravityQuery.TryGetComponent(comp.Owner, out var gravity))
                {
                    RemCompDeferred<GravityShakeComponent>(comp.Owner);
                    continue;
                }

                ShakeGrid(comp.Owner, gravity);
                comp.ShakeTimes--;
                comp.NextShake += TimeSpan.FromSeconds(ShakeCooldown);
                Dirty(comp);
            }
        }
    }

    public void StartGridShake(EntityUid uid, GravityComponent? gravity = null)
    {
        if (Terminating(uid))
            return;

        if (!Resolve(uid, ref gravity, false))
            return;

        if (!TryComp<GravityShakeComponent>(uid, out var shake))
        {
            shake = AddComp<GravityShakeComponent>(uid);
            shake.NextShake = Timing.CurTime;
        }

        shake.ShakeTimes = 10;
        Dirty(shake);
    }

    protected virtual void ShakeGrid(EntityUid uid, GravityComponent? comp = null) {}

    private void OnShakeHandleState(EntityUid uid, GravityShakeComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not GravityShakeComponentState state)
            return;

        component.ShakeTimes = state.ShakeTimes;
        component.NextShake = state.NextShake;
    }

    private void OnShakeGetState(EntityUid uid, GravityShakeComponent component, ref ComponentGetState args)
    {
        args.State = new GravityShakeComponentState()
        {
            ShakeTimes = component.ShakeTimes,
            NextShake = component.NextShake,
        };
    }

    [Serializable, NetSerializable]
    protected sealed class GravityShakeComponentState : ComponentState
    {
        public int ShakeTimes;
        public TimeSpan NextShake;
    }
}
