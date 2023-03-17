using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.HotPotato;

public abstract class SharedHotPotatoSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HotPotatoComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<HotPotatoComponent, ComponentHandleState>(HandleCompState);
    }

    private void GetCompState(EntityUid uid, HotPotatoComponent comp, ref ComponentGetState args)
    {
        args.State = new HotPotatoComponentState(comp.CanTransfer);
    }

    private void HandleCompState(EntityUid uid, HotPotatoComponent comp, ref ComponentHandleState args)
    {
        if (args.Current is not HotPotatoComponentState state)
            return;
        comp.CanTransfer = state.CanTransfer;
    }

    [Serializable, NetSerializable]
    public sealed class HotPotatoComponentState : ComponentState
    {
        public bool CanTransfer;
        
        public HotPotatoComponentState(bool canTransfer)
        {
            CanTransfer = canTransfer;
        }
    }
}