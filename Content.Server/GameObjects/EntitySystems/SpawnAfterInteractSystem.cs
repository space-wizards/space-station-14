using Content.Server.GameObjects.Components.Engineering;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.GameObjects.EntitySystems.DoAfter;
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

        private async void HandleAfterInteract(EntityUid uid, SpawnAfterInteractComponent component, AfterInteractMessage args)
        {
            if (component.Prototype == null)
                return;
            if(!_mapManager.TryGetGrid(args.ClickLocation.GetGridId(component.Owner.EntityManager), out var grid))
                return;
            if (!args.InRangeUnobstructed(ignoreInsideBlocker: true, popup: true))
                return;
            var snapPos = grid.SnapGridCellFor(args.ClickLocation, SnapGridOffset.Center);

            bool CheckTileClear()
            {
                return !grid.GetTileRef(snapPos).Tile.IsEmpty && args.User.InRangeUnobstructed(args.ClickLocation);
            }

            if (component.DoAfterTime > 0 && TryGet<DoAfterSystem>(out var doAfterSystem))
            {
                var doAfterArgs = new DoAfterEventArgs(args.User, component.DoAfterTime)
                {
                    BreakOnUserMove = true,
                    BreakOnStun = true,
                    ExtraCheck = CheckTileClear,
                };
                var result = await doAfterSystem.DoAfter(doAfterArgs);

                if (result != DoAfterStatus.Finished)
                    return;
            }

            StackComponent? stack = null;
            if (component.RemoveOnInteract && component.Owner.TryGetComponent(out stack) && !stack.Use(1))
                return;

            EntityManager.SpawnEntity(component.Prototype, grid.GridTileToLocal(snapPos));

            if (component.RemoveOnInteract && stack == null && !component.Owner.Deleted)
                component.Owner.Delete();

            return;
        }
    }
}
