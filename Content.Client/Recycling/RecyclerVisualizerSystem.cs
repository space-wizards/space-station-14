using Content.Shared.Conveyor;
using Content.Shared.Recycling;
using Robust.Client.GameObjects;

namespace Content.Client.Recycling
{
    public sealed class RecyclerVisualizerSystem : VisualizerSystem<RecyclerComponent>
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RecyclerComponent, ComponentInit>(OnComponentInit);
        }

        private void OnComponentInit(EntityUid uid, RecyclerComponent recycler, ComponentInit args)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(uid, out ISpriteComponent? sprite) ||
                !entMan.TryGetComponent(uid, out AppearanceComponent? appearance))
            {
                return;
            }

            UpdateAppearance(uid, recycler, sprite);
        }

        protected override void OnAppearanceChange(EntityUid uid, RecyclerComponent recycler, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
            {
                return;
            }

            UpdateAppearance(uid, recycler, args.Sprite);
        }

        private void UpdateAppearance(EntityUid uid, RecyclerComponent recycler, ISpriteComponent sprite)
        {
            var state = recycler.SpriteLayerOff;
            if (AppearanceSystem.TryGetData<ConveyorState>(uid, ConveyorVisuals.State, out var conveyorState) && conveyorState != ConveyorState.Off)
            {
                state = recycler.SpriteLayerOn;
            }

            if (AppearanceSystem.TryGetData<bool>(uid, RecyclerVisuals.Bloody, out var bloody) && bloody)
            {
                state += "bld";
            }

            sprite.LayerSetState(RecyclerVisualLayers.Main, state);
        }
    }

    public enum RecyclerVisualLayers : byte
    {
        Main
    }
}
