using Content.Shared.Buckle.Components;
using Content.Shared.DragDrop;

namespace Content.Client.Buckle.Strap
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedStrapComponent))]
    public sealed class StrapComponent : SharedStrapComponent
    {
        public override bool DragDropOn(DragDropEvent eventArgs)
        {
            return false;
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);
            if (curState is not StrapComponentState state) return;
            Position = state.Position;
            BuckleOffsetUnclamped = state.BuckleOffsetClamped;
            BuckledEntities.Clear();
            BuckledEntities.UnionWith(state.BuckledEntities);
            MaxBuckleDistance = state.MaxBuckleDistance;
        }
    }
}
