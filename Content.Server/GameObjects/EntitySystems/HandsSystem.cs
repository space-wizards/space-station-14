using System;
using Content.Server.GameObjects.Components.Projectiles;
using Content.Shared.Input;
using SS14.Server.GameObjects;
using SS14.Server.GameObjects.EntitySystems;
using SS14.Server.Interfaces.Player;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.EntitySystemMessages;
using SS14.Shared.GameObjects.Systems;
using SS14.Shared.Input;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    internal class HandsSystem : EntitySystem
    {
        private const float ThrowSpeed = 1.0f;

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

            if (!msg.Entity.TryGetComponent(out ITransformComponent transform))
                return;

            // if item is in a container
            if(transform.IsMapTransform)
                return;

            if(!msg.Entity.TryGetComponent(out PhysicsComponent physics))
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

        private static void HandleDrop(ICommonSession session, GridLocalCoordinates coords, EntityUid uid)
        {
            var ent = ((IPlayerSession) session).AttachedEntity;

            if(ent == null || !ent.IsValid())
                return;

            if (!ent.TryGetComponent(out HandsComponent handsComp))
                return;

            var transform = ent.Transform;

            GridLocalCoordinates? dropPos = null;
            if (transform.LocalPosition.InRange(coords, InteractionSystem.INTERACTION_RANGE))
            {
                dropPos = coords;
            }

            handsComp.Drop(handsComp.ActiveIndex, dropPos);
        }

        private static void HandleActivateItem(ICommonSession session)
        {
            if (!TryGetAttachedComponent(session as IPlayerSession, out HandsComponent handsComp))
                return;

            handsComp.ActivateItem();
        }

        private static void HandleThrowItem(ICommonSession session, GridLocalCoordinates coords, EntityUid uid)
        {
            var plyEnt = ((IPlayerSession)session).AttachedEntity;

            if (plyEnt == null || !plyEnt.IsValid())
                return;

            if (!plyEnt.TryGetComponent(out HandsComponent handsComp))
                return;

            if (handsComp.CanDrop(handsComp.ActiveIndex))
            {
                var throwEnt = handsComp.GetHand(handsComp.ActiveIndex).Owner;
                handsComp.Drop(handsComp.ActiveIndex, null);

                if (!throwEnt.TryGetComponent(out ProjectileComponent projComp))
                {
                    projComp = throwEnt.AddComponent<ProjectileComponent>();
                }
            
                projComp.IgnoreEntity(plyEnt);

                var transform = plyEnt.Transform;
                var dirVec = (coords.ToWorld().Position - transform.WorldPosition).Normalized;

                if (!throwEnt.TryGetComponent(out PhysicsComponent physComp))
                {
                    physComp = throwEnt.AddComponent<PhysicsComponent>();
                }

                physComp.LinearVelocity = dirVec * ThrowSpeed;


                var wHomoDir = Vector3.UnitX;

                transform.InvWorldMatrix.Transform(ref wHomoDir, out var lHomoDir);

                lHomoDir.Normalize();
                var angle = new Angle(lHomoDir.Xy);

                transform.LocalRotation = angle;
            }
            else
            {
                return;
            }
        }
    }
}
