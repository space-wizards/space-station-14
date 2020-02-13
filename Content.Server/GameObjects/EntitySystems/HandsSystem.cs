using System;
using Content.Server.GameObjects.Components;
using Content.Server.GameObjects.Components.Stack;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Input;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystemMessages;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Interfaces.Physics;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class HandsSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

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
            SubscribeEvent<EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeEvent<EntInsertedIntoContainerMessage>(HandleContainerModified);
        }

        private static void HandleContainerModified(object sender, ContainerModifiedMessage args)
        {
            if (args.Container.Owner.TryGetComponent(out IHandsComponent handsComponent))
            {
                handsComponent.HandleSlotModifiedMaybe(args);
            }
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

            var interactionSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<InteractionSystem>();

            var oldItem = handsComp.GetActiveHand;

            handsComp.SwapHands();

            var newItem = handsComp.GetActiveHand;

            if(oldItem != null)
                interactionSystem.HandDeselectedInteraction(handsComp.Owner, oldItem.Owner);

            if(newItem != null)
                interactionSystem.HandSelectedInteraction(handsComp.Owner, newItem.Owner);
        }

        private bool HandleDrop(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            var ent = ((IPlayerSession) session).AttachedEntity;

            if (ent == null || !ent.IsValid())
                return false;

            if (!ent.TryGetComponent(out HandsComponent handsComp))
                return false;

            if (handsComp.GetActiveHand == null)
                return false;

            var dir = (coords.Position - ent.Transform.GridPosition.Position);
            var ray = new CollisionRay(ent.Transform.GridPosition.Position, dir.Normalized, (int) CollisionGroup.Impassable);
            var rayResults = IoCManager.Resolve<IPhysicsManager>().IntersectRay(ent.Transform.MapID, ray, dir.Length, ent);

            if(!rayResults.DidHitObject)
                if (coords.InRange(_mapManager, ent.Transform.GridPosition, InteractionSystem.InteractionRange))
                {
                    handsComp.Drop(handsComp.ActiveIndex, coords);
                }
                else
                {
                    var entCoords = ent.Transform.GridPosition.Position;
                    var entToDesiredDropCoords = coords.Position - entCoords;
                    var clampedDropCoords = ((entToDesiredDropCoords.Normalized * InteractionSystem.InteractionRange) + entCoords);

                    handsComp.Drop(handsComp.ActiveIndex, new GridCoordinates(clampedDropCoords, coords.GridID));
                }
            else
                handsComp.Drop(handsComp.ActiveIndex, ent.Transform.GridPosition);

            return true;
        }

        private static void HandleActivateItem(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            handsComp.ActivateItem();
        }

        private bool HandleThrowItem(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            var plyEnt = ((IPlayerSession)session).AttachedEntity;

            if (plyEnt == null || !plyEnt.IsValid())
                return false;

            if (!plyEnt.TryGetComponent(out HandsComponent handsComp))
                return false;

            if (!handsComp.CanDrop(handsComp.ActiveIndex))
                return false;

            var throwEnt = handsComp.GetHand(handsComp.ActiveIndex).Owner;

            if (!handsComp.ThrowItem())
                return false;

            // pop off an item, or throw the single item in hand.
            if (!throwEnt.TryGetComponent(out StackComponent stackComp) || stackComp.Count < 2)
            {
                handsComp.Drop(handsComp.ActiveIndex);
            }
            else
            {
                stackComp.Use(1);
                throwEnt = throwEnt.EntityManager.SpawnEntity(throwEnt.Prototype.ID, plyEnt.Transform.GridPosition);

                // can only throw one item at a time, regardless of what the prototype stack size is.
                if (throwEnt.TryGetComponent<StackComponent>(out var newStackComp))
                    newStackComp.Count = 1;
            }

            if (!throwEnt.TryGetComponent(out CollidableComponent colComp))
                return true;

            colComp.CollisionEnabled = true;
            // I can now collide with player, so that i can do damage.

            if (!throwEnt.TryGetComponent(out ThrownItemComponent projComp))
            {
                projComp = throwEnt.AddComponent<ThrownItemComponent>();

                if(colComp.PhysicsShapes.Count == 0)
                    colComp.PhysicsShapes.Add(new PhysShapeAabb());

                colComp.PhysicsShapes[0].CollisionMask |= (int)CollisionGroup.MobImpassable;
                colComp.IsScrapingFloor = false;
            }

            projComp.User = plyEnt;
            projComp.IgnoreEntity(plyEnt);

            var transform = plyEnt.Transform;
            var dirVec = (coords.ToMapPos(_mapManager) - transform.WorldPosition).Normalized;

            if (!throwEnt.TryGetComponent(out PhysicsComponent physComp))
                physComp = throwEnt.AddComponent<PhysicsComponent>();

            // TODO: Move this into PhysicsSystem, we need an ApplyForce function.
            var a = ThrowForce / (float) Math.Max(0.001, physComp.Mass); // a = f / m

            var timing = IoCManager.Resolve<IGameTiming>();
            var spd = a / (1f / timing.TickRate); // acceleration is applied in 1 tick instead of 1 second, scale appropriately

            physComp.LinearVelocity = dirVec * spd;

            transform.LocalRotation = new Angle(dirVec).GetCardinalDir().ToAngle();

            return true;
        }
    }
}
