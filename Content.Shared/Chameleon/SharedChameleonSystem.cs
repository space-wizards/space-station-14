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
            chameleon.StealthLevel = Math.Clamp(chameleon.StealthLevel - frameTime * chameleon.InvisibilityRate, -1f, 1f);

            Dirty(chameleon);
        }
    }

    private void OnChameleonGetState(EntityUid uid, SharedChameleonComponent component, ref ComponentGetState args)
    {
        args.State = new ChameleonComponentState(component.StealthLevel);
    }

    private void OnChameleonHandlesState(EntityUid uid, SharedChameleonComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not ChameleonComponentState cast)
            return;

        component.StealthLevel = cast.StealthLevel;
    }

    private void OnMove(EntityUid uid, SharedChameleonComponent component, ref MoveEvent args)
    {
        if (args.NewPosition.EntityId != args.OldPosition.EntityId)
            return;

        component.StealthLevel += component.VisibilityRate*(args.NewPosition.Position - args.OldPosition.Position).Length;
        component.StealthLevel = Math.Clamp(component.StealthLevel, -1f, 1f);

        Dirty(component);
    }
}
