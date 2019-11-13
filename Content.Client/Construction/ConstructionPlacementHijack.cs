using Content.Client.GameObjects.Components.Construction;
using Content.Shared.Construction;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Placement;
using Robust.Client.Utility;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;

namespace Content.Client.Construction
{
    public class ConstructionPlacementHijack : PlacementHijack
    {
        private readonly ConstructionPrototype Prototype;
        private readonly ConstructorComponent Owner;

        public ConstructionPlacementHijack(ConstructionPrototype prototype, ConstructorComponent owner)
        {
            Prototype = prototype;
            Owner = owner;
        }

        public override bool HijackPlacementRequest(GridCoordinates coords)
        {
            if (Prototype != null)
            {
                var dir = Manager.Direction;
                Owner.SpawnGhost(Prototype, coords, dir);
            }
            return true;
        }

        public override bool HijackDeletion(IEntity entity)
        {
            if (entity.TryGetComponent(out ConstructionGhostComponent ghost))
            {
                Owner.ClearGhost(ghost.GhostID);
            }
            return true;
        }

        public override void StartHijack(PlacementManager manager)
        {
            base.StartHijack(manager);

            manager.CurrentBaseSprite = Prototype.Icon.DirFrame0();
        }
    }
}
