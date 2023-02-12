using Content.Server.Coordinates.Helpers;
using Content.Server.DoAfter;
using Content.Server.Engineering.Components;
using Content.Server.Stack;
using Content.Shared.Interaction;
using Content.Shared.Maps;
using Content.Shared.Stacks;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Server.Engineering.EntitySystems
{
    [UsedImplicitly]
    public sealed class SpawnAfterInteractSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SpawnAfterInteractComponent, AfterInteractEvent>(HandleAfterInteract);
        }

        private async void HandleAfterInteract(EntityUid uid, SpawnAfterInteractComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach && !component.IgnoreDistance)
                return;
            if (string.IsNullOrEmpty(component.Prototype))
                return;
            if (!_mapManager.TryGetGrid(args.ClickLocation.GetGridUid(EntityManager), out var grid))
                return;
            if (!grid.TryGetTileRef(args.ClickLocation, out var tileRef))
                return;

            bool IsTileClear()
            {
                return tileRef.Tile.IsEmpty == false && !tileRef.IsBlockedTurf(true);
            }

            if (!IsTileClear())
                return;

            if (component.DoAfterTime > 0)
            {
                var doAfterArgs = new DoAfterEventArgs(args.User, component.DoAfterTime)
                {
                    BreakOnUserMove = true,
                    BreakOnStun = true,
                    PostCheck = IsTileClear,
                };
                var result = await _doAfterSystem.WaitDoAfter(doAfterArgs);

                if (result != DoAfterStatus.Finished)
                    return;
            }

            if (component.Deleted || Deleted(component.Owner))
                return;

            if (EntityManager.TryGetComponent<StackComponent?>(component.Owner, out var stackComp)
                && component.RemoveOnInteract && !_stackSystem.Use(uid, 1, stackComp))
            {
                return;
            }

            EntityManager.SpawnEntity(component.Prototype, args.ClickLocation.SnapToGrid(grid));

            if (component.RemoveOnInteract && stackComp == null && !((!EntityManager.EntityExists(component.Owner) ? EntityLifeStage.Deleted : EntityManager.GetComponent<MetaDataComponent>(component.Owner).EntityLifeStage) >= EntityLifeStage.Deleted))
                EntityManager.DeleteEntity(component.Owner);
        }
    }
}
