using Content.Client.GameObjects.Components.Construction;
using Content.Shared.Construction;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.ResourceManagement;
using SS14.Client.Placement;
using SS14.Client.ResourceManagement;
using SS14.Client.Utility;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

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

        public override bool HijackPlacementRequest(GridLocalCoordinates coords)
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

            var res = IoCManager.Resolve<IResourceCache>();
            manager.CurrentBaseSprite = Prototype.Icon.DirFrame0();
        }
    }
}
