using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Medical.Surgery;

public abstract class SharedSurgeryRealmSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesBefore.Add(typeof(SharedInputSystem));

        SubscribeLocalEvent<SurgeryRealmSlidingComponent, ComponentGetState>(OnSlidingGetState);
        SubscribeLocalEvent<SurgeryRealmSlidingComponent, ComponentHandleState>(OnSlidingHandleState);

        SubscribeLocalEvent<SurgeryRealmHeartComponent, ComponentGetState>(OnHeartGetState);
        SubscribeLocalEvent<SurgeryRealmHeartComponent, ComponentHandleState>(OnHeartHandleState);
    }

    private void OnSlidingGetState(EntityUid uid, SurgeryRealmSlidingComponent component, ref ComponentGetState args)
    {
        args.State = new SurgeryRealmSlidingComponentState(component.FinalY, component.SectionPos);
    }

    private void OnSlidingHandleState(EntityUid uid, SurgeryRealmSlidingComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SurgeryRealmSlidingComponentState state)
            return;

        component.FinalY = state.FinalY;
        component.SectionPos = state.SectionPos;
    }

    private void OnHeartGetState(EntityUid uid, SurgeryRealmHeartComponent component, ref ComponentGetState args)
    {
        args.State = new SurgeryRealmHeartComponentState(component.Health);
    }

    private void OnHeartHandleState(EntityUid uid, SurgeryRealmHeartComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SurgeryRealmHeartComponentState state)
            return;

        component.Health = state.Health;
    }

    protected virtual void Fire(SurgeryRealmSlidingComponent sliding)
    {
    }

    public override void Update(float frameTime)
    {
        foreach (var sliding in EntityQuery<SurgeryRealmSlidingComponent>())
        {
            if (!sliding.Fired &&
                _transform.GetWorldPosition(sliding.Owner).Y - sliding.SectionPos.Y < sliding.FinalY)
            {
                Physics.SetLinearVelocity(sliding.Owner, (0, 0));
                Fire(sliding);
            }
        }

        // foreach (var heart in EntityQuery<SurgeryRealmHeartComponent>())
        // {
        //     if (!TryComp(heart.Owner, out InputMoverComponent? input))
        //         continue;
        //
        //     var newY = _transform.GetWorldPosition(heart.Owner).Y;
        //
        //     if (heart.Falling)
        //     {
        //         if (Math.Abs(heart.LastY - newY) < 0.0001)
        //         {
        //             heart.Falling = false;
        //         }
        //     }
        //
        //     heart.LastY = newY;
        //
        //     if (heart.Falling)
        //     {
        //         input.HeldMoveButtons &= ~MoveButtons.Up;
        //         input.HeldMoveButtons |= MoveButtons.Down;
        //     }
        //     else
        //     {
        //         if ((input.HeldMoveButtons & MoveButtons.Up) != 0)
        //         {
        //             input.HeldMoveButtons &= ~MoveButtons.Down;
        //         }
        //         else
        //         {
        //             heart.Falling = true;
        //         }
        //     }
        // }
    }
}
