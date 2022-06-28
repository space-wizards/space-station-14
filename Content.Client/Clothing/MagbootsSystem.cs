using Content.Shared.Clothing;
using Robust.Shared.GameStates;
using static Content.Shared.Clothing.SharedMagbootsComponent;

namespace Content.Client.Clothing;

public sealed class MagbootsSystem : SharedMagbootsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MagbootsComponent, ComponentHandleState>(OnHandleState);
    }

    private void OnHandleState(EntityUid uid, MagbootsComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not MagbootsComponentState componentState)
            return;

        component.On = componentState.On;
        OnChanged(component);
    }
}

