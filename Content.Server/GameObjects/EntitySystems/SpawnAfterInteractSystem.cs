#nullable enable
using Content.Server.GameObjects.Components.Engineering;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Server.Utility;
using Content.Shared.Interfaces.GameObjects.Components;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SpawnAfterInteractSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnAfterInteractComponent, AfterInteractMessage>(HandleAfterInteract);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<SpawnAfterInteractComponent, AfterInteractMessage>(HandleAfterInteract);
        }

        private async void HandleAfterInteract(EntityUid uid, SpawnAfterInteractComponent component, AfterInteractMessage args)
        {
            if (string.IsNullOrEmpty(component.Prototype))
                return;
            if (!_mapManager.TryGetGrid(args.ClickLocation.GetGridId(EntityManager), out var grid))
                return;
            if (!grid.TryGetTileRef(args.ClickLocation, out var tileRef))
                return;

            bool IsTileClear()
            {
                return tileRef.Tile.IsEmpty == false && args.User.InRangeUnobstructed(args.ClickLocation, popup: true);
            }

            if (!IsTileClear())
                return;

            if (component.DoAfterTime > 0 && TryGet<DoAfterSystem>(out var doAfterSystem))
            {
                var doAfterArgs = new DoAfterEventArgs(args.User, component.DoAfterTime)
                {
                    BreakOnUserMove = true,
                    BreakOnStun = true,
                    PostCheck = IsTileClear,
                };
                var result = await doAfterSystem.DoAfter(doAfterArgs);

                if (result != DoAfterStatus.Finished)
                    return;
            }

            if (component.Deleted || component.Owner.Deleted)
                return;

            StackComponent? stack = null;
            if (component.RemoveOnInteract && component.Owner.TryGetComponent(out stack) && !stack.Use(1))
                return;

            EntityManager.SpawnEntity(component.Prototype, args.ClickLocation.SnapToGrid(grid));

            if (component.RemoveOnInteract && stack == null && !component.Owner.Deleted)
                component.Owner.Delete();
        }
    }
}
