using Content.Client.Buckle.Strap;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Robust.Shared.GameStates;

namespace Content.Client.Buckle
{
    internal sealed class BuckleSystem : SharedBuckleSystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<StrapComponent, ComponentHandleState>(OnStrapHandleState);
        }

        private void OnStrapHandleState(EntityUid uid, StrapComponent component, ref ComponentHandleState args)
        {
            if (args.Current is not StrapComponentState state) return;
            component.Position = state.Position;
            component.BuckleOffsetUnclamped = state.BuckleOffsetClamped;
            component.BuckledEntities.Clear();
            component.BuckledEntities.UnionWith(state.BuckledEntities);
            component.MaxBuckleDistance = state.MaxBuckleDistance;
        }
    }
}
