using System.Collections.Generic;
using Content.Client.Construction;
using Content.Shared.Construction;
using Content.Shared.GameObjects.Components.Construction;
using SS14.Client.GameObjects;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace Content.Client.GameObjects.Components.Construction
{
    public class ConstructorComponent : SharedConstructorComponent
    {
        int nextId;
        readonly Dictionary<int, ConstructionGhostComponent> Ghosts = new Dictionary<int, ConstructionGhostComponent>();
        ConstructionButton Button;

        ITransformComponent Transform;

        public override void Initialize()
        {
            base.Initialize();

            Transform = Owner.GetComponent<ITransformComponent>();
        }

        public override void HandleMessage(ComponentMessage message, INetChannel netChannel = null, IComponent component = null)
        {
            base.HandleMessage(message, netChannel, component);

            switch (message)
            {
                case PlayerAttachedMsg _:
                    if (Button == null)
                    {
                        Button = new ConstructionButton();
                        Button.Owner = this;
                    }
                    Button.AddToScreen();
                    break;

                case PlayerDetachedMsg _:
                    Button.RemoveFromScreen();
                    break;
            }
        }

        public override void OnRemove()
        {
            Button?.Dispose();
        }

        public void SpawnGhost(ConstructionPrototype prototype, GridLocalCoordinates loc)
        {
            var entMgr = IoCManager.Resolve<IClientEntityManager>();
            var ghost = entMgr.ForceSpawnEntityAt("constructionghost", Transform.LocalPosition);
            var comp = ghost.GetComponent<ConstructionGhostComponent>();
            comp.Prototype = prototype;
            comp.Master = this;
            comp.GhostID = nextId++;
            var sprite = ghost.GetComponent<SpriteComponent>();
            sprite.LayerSetTexture(0, prototype.Icon);
            sprite.LayerSetShader(0, "unshaded");

            Ghosts[comp.GhostID] = comp;
        }

        public void TryStartConstruction(int ghostId)
        {
            var ghost = Ghosts[ghostId];
            var msg = new TryStartStructureConstructionMessage(ghost.Owner.GetComponent<ITransformComponent>().LocalPosition, ghost.Prototype.ID);

        }

        /*
                    var ent = IoCManager.Resolve<IClientEntityManager>();
            var ghost = ent.ForceSpawnEntityAt("constructionghost", Owner.Owner.GetComponent<ITransformComponent>().LocalPosition);
            var ghostComp = ghost.GetComponent<ConstructionGhostComponent>();
            ghostComp.Prototype = prototype;
            ghostComp.Master = Owner;
            var sprite = ghost.GetComponent<SpriteComponent>();
            sprite.LayerSetTexture(0, prototype.Icon);
            sprite.LayerSetShader(0, "unshaded"); */
    }
}
