using System;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Input;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    internal class HandsSystem : EntitySystem
    {
        private const float ThrowForce = 1.5f; // Throwing force of mobs in Newtons

        /// <inheritdoc />
        public override void Initialize()
        {
            base.Initialize();

            var input = EntitySystemManager.GetEntitySystem<InputSystem>();
            input.BindMap.BindFunction(ContentKeyFunctions.SwapHands, InputCmdHandler.FromDelegate(HandleSwapHands));
            input.BindMap.BindFunction(ContentKeyFunctions.Drop, new PointerInputCmdHandler(HandleDrop));
            input.BindMap.BindFunction(ContentKeyFunctions.ActivateItemInHand, InputCmdHandler.FromDelegate(HandleActivateItem));
            input.BindMap.BindFunction(ContentKeyFunctions.ThrowItemInHand, new PointerInputCmdHandler(HandleThrowItem));
        }

        /// <inheritdoc />
        public override void Shutdown()
        {
            if (EntitySystemManager.TryGetEntitySystem(out InputSystem input))
            {
                input.BindMap.UnbindFunction(ContentKeyFunctions.SwapHands);
                input.BindMap.UnbindFunction(ContentKeyFunctions.Drop);
                input.BindMap.UnbindFunction(ContentKeyFunctions.ActivateItemInHand);
                input.BindMap.UnbindFunction(ContentKeyFunctions.ThrowItemInHand);
            }

            base.Shutdown();
        }

        /// <inheritdoc />
        public override void SubscribeEvents()
        {
            SubscribeEvent<EntParentChangedMessage>(HandleParented);
        }

        private static void HandleParented(object sender, EntitySystemMessage args)
        {
            var msg = (EntParentChangedMessage) args;

            // entity is no longer a child of OldParent, therefore it cannot be in the hand of the parent
            if (msg.OldParent != null && msg.OldParent.IsValid() && msg.OldParent.Transform != msg.Entity.Transform.Parent && msg.OldParent.TryGetComponent(out IHandsComponent handsComp))
            {
                handsComp.RemoveHandEntity(msg.Entity);
            }

            if (msg.Entity.Deleted)
                return;

            // if item is in a container
            if (msg.Entity.Transform.IsMapTransform)
                return;

            if (!msg.Entity.TryGetComponent(out PhysicsComponent physics))
                return;

            // set velocity to zero
            physics.LinearVelocity = Vector2.Zero;
        }

        private static bool TryGetAttachedComponent<T>(IPlayerSession session, out T component)
            where T : Component
        {
            component = default;

            var ent = session.AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out T comp))
                return false;

            component = comp;
            return true;
        }

        private static void HandleSwapHands(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            handsComp.SwapHands();
        }

        private static void HandleDrop(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            var ent = ((IPlayerSession) session).AttachedEntity;

            if(ent == null || !ent.IsValid())
                return;

            if (!ent.TryGetComponent(out HandsComponent handsComp))
                return;

            var transform = ent.Transform;

            if (transform.GridPosition.InRange(coords, InteractionSystem.INTERACTION_RANGE))
            {
                handsComp.Drop(handsComp.ActiveIndex, coords);
            }
            else
            {
                handsComp.Drop(handsComp.ActiveIndex);
            }
        }

        private static void HandleActivateItem(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            handsComp.ActivateItem();
        }

        private static void HandleThrowItem(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            var plyEnt = ((IPlayerSession)session).AttachedEntity;

            if (plyEnt == null || !plyEnt.IsValid())
                return;

            if (!plyEnt.TryGetComponent(out HandsComponent handsComp))
                return;

            if (!handsComp.CanDrop(handsComp.ActiveIndex))
                return;

            var throwEnt = handsComp.GetHand(handsComp.ActiveIndex).Owner;

            // pop off an item, or throw the single item in hand.
            if (!throwEnt.TryGetComponent(out StackComponent stackComp) || stackComp.Count < 2)
            {
                handsComp.Drop(handsComp.ActiveIndex);
            }
            else
            {
                stackComp.Use(1);
                throwEnt = throwEnt.EntityManager.ForceSpawnEntityAt(throwEnt.Prototype.ID, plyEnt.Transform.GridPosition);
            }

            if (!throwEnt.TryGetComponent(out CollidableComponent colComp))
            {
                colComp = throwEnt.AddComponent<CollidableComponent>();

                if(!colComp.Running)
                    colComp.Startup();
            }

            colComp.CollisionEnabled = true;
            colComp.CollisionLayer |= (int)CollisionGroup.Items;
            colComp.CollisionMask |= (int)CollisionGroup.Grid;

            // I can now collide with player, so that i can do damage.
            colComp.CollisionMask |= (int) CollisionGroup.Mob;

            if (!throwEnt.TryGetComponent(out ThrownItemComponent projComp))
            {
                projComp = throwEnt.AddComponent<ThrownItemComponent>();
            }

            projComp.IgnoreEntity(plyEnt);

            var transform = plyEnt.Transform;
            var dirVec = (coords.ToWorld().Position - transform.WorldPosition).Normalized;

            if (!throwEnt.TryGetComponent(out PhysicsComponent physComp))
            {
                physComp = throwEnt.AddComponent<PhysicsComponent>();
            }

            // TODO: Move this into PhysicsSystem, we need an ApplyForce function.
            var a = ThrowForce / (float) Math.Max(0.001, physComp.Mass); // a = f / m

            var timing = IoCManager.Resolve<IGameTiming>();
            var spd = a / (1f / timing.TickRate); // acceleration is applied in 1 tick instead of 1 second, scale appropriately

            physComp.LinearVelocity = dirVec * spd;

            var wHomoDir = Vector3.UnitX;

            transform.InvWorldMatrix.Transform(ref wHomoDir, out var lHomoDir);

            lHomoDir.Normalize();
            transform.LocalRotation = new Angle(lHomoDir.Xy);
        }
    }
}
