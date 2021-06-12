#nullable enable
using Content.Server.Coordinates.Helpers;
using Content.Server.DoAfter;
using Content.Server.Engineering.Components;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Server.Engineering.EntitySystems
{
    [UsedImplicitly]
    public class SpawnAfterInteractSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnAfterInteractComponent, AfterInteractEvent>(HandleAfterInteract);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            UnsubscribeLocalEvent<SpawnAfterInteractComponent, AfterInteractEvent>(HandleAfterInteract);
        }

        private async void HandleAfterInteract(EntityUid uid, SpawnAfterInteractComponent component, AfterInteractEvent args)
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

            if (component.Owner.TryGetComponent<SharedStackComponent>(out var stackComp)
                && component.RemoveOnInteract && !Get<StackSystem>().Use(uid, stackComp, 1))
            {
                return;
            }

            EntityManager.SpawnEntity(component.Prototype, args.ClickLocation.SnapToGrid(grid));

            if (component.RemoveOnInteract && stackComp == null && !component.Owner.Deleted)
                component.Owner.Delete();
        }
    }
}
