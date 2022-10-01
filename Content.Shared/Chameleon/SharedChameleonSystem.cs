using Content.Shared.Chameleon.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Timing;

namespace Content.Shared.Chameleon;

public abstract class SharedChameleonSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedChameleonComponent, ComponentGetState>(OnChameleonGetState);
        SubscribeLocalEvent<SharedChameleonComponent, ComponentHandleState>(OnChameleonHandlesState);
        SubscribeLocalEvent<SharedChameleonComponent, MoveEvent>(OnMove);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var chameleon in EntityQuery<SharedChameleonComponent>())
        {
            chameleon.Speed = Math.Clamp(chameleon.Speed - frameTime * 0.15f, -1f, 1f);

            Dirty(chameleon);
        }
    }

    private void OnChameleonGetState(EntityUid uid, SharedChameleonComponent component, ref ComponentGetState args)
    {
        args.State = new ChameleonComponentState(component.HadOutline, component.Speed);
    }

    private void OnChameleonHandlesState(EntityUid uid, SharedChameleonComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ChameleonComponentState cast)
            return;

        component.HadOutline = cast.HadOutline;
        component.Speed = cast.Speed;
    }

    private void OnMove(EntityUid uid, SharedChameleonComponent component, ref MoveEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.NewPosition.EntityId != args.OldPosition.EntityId)
            return;

        component.Speed += 0.2f*(args.NewPosition.Position - args.OldPosition.Position).Length;
        component.Speed = Math.Clamp(component.Speed, -1f, 1f);

        Dirty(component);
    }
}
