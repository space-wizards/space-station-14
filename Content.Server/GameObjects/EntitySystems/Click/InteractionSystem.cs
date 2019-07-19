using System;
using System.Linq;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.Interfaces.GameObjects;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Players;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    /// This interface gives components behavior when being clicked on or "attacked" by a user with an object in their hand
    /// </summary>
    public interface IAttackBy
    {
        /// <summary>
        /// Called when using one object on another
        /// </summary>
        bool AttackBy(AttackByEventArgs eventArgs);
    }

    public class AttackByEventArgs : EventArgs
    {
        public IEntity User { get; set; }
        public GridCoordinates ClickLocation { get; set; }
        public IEntity AttackWith { get; set; }
    }

    /// <summary>
    /// This interface gives components behavior when being clicked on or "attacked" by a user with an empty hand
    /// </summary>
    public interface IAttackHand
    {
        /// <summary>
        /// Called when a player directly interacts with an empty hand
        /// </summary>
        bool AttackHand(AttackHandEventArgs eventArgs);
    }

    public class AttackHandEventArgs : EventArgs
    {
        public IEntity User { get; set; }
    }

    /// <summary>
    /// This interface gives components behavior when being clicked by objects outside the range of direct use
    /// </summary>
    public interface IRangedAttackBy
    {
        /// <summary>
        /// Called when we try to interact with an entity out of range
        /// </summary>
        /// <returns></returns>
        bool RangedAttackBy(RangedAttackByEventArgs eventArgs);
    }

    [PublicAPI]
    public class RangedAttackByEventArgs : EventArgs
    {
        public IEntity User { get; set; }
        public IEntity Weapon { get; set; }
        public GridCoordinates ClickLocation { get; set; }
    }

    /// <summary>
    /// This interface gives components a behavior when clicking on another object and no interaction occurs
    /// Doesn't pass what you clicked on as an argument, but if it becomes necessary we can add it later
    /// </summary>
    public interface IAfterAttack
    {
        /// <summary>
        /// Called when we interact with nothing, or when we interact with an entity out of range that has no behavior
        /// </summary>
        void AfterAttack(AfterAttackEventArgs eventArgs);
    }

    public class AfterAttackEventArgs : EventArgs
    {
        public IEntity User { get; set; }
        public GridCoordinates ClickLocation { get; set; }
        public IEntity Attacked { get; set; }
    }

    /// <summary>
    /// This interface gives components behavior when using the entity in your hands
    /// </summary>
    public interface IUse
    {
        /// <summary>
        /// Called when we activate an object we are holding to use it
        /// </summary>
        /// <returns></returns>
        bool UseEntity(UseEntityEventArgs eventArgs);
    }

    public class UseEntityEventArgs : EventArgs
    {
        public IEntity User { get; set; }
    }

    /// <summary>
    ///     This interface gives components behavior when being activated in the world.
    /// </summary>
    public interface IActivate
    {
        /// <summary>
        ///     Called when this component is activated by another entity.
        /// </summary>
        void Activate(ActivateEventArgs eventArgs);
    }

    public class ActivateEventArgs : EventArgs
    {
        public IEntity User { get; set; }
    }

    /// <summary>
    ///     This interface gives components behavior when thrown.
    /// </summary>
    public interface IThrown
    {
        void Thrown(ThrownEventArgs eventArgs);
    }

    public class ThrownEventArgs : EventArgs
    {
        public ThrownEventArgs(IEntity user)
        {
            User = user;
        }

        public IEntity User { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when landing after being thrown.
    /// </summary>
    public interface ILand
    {
        void Land(LandEventArgs eventArgs);
    }

    public class LandEventArgs : EventArgs
    {
        public LandEventArgs(IEntity user, GridCoordinates landingLocation)
        {
            User = user;
            LandingLocation = landingLocation;
        }

        public IEntity User { get; }
        public GridCoordinates LandingLocation { get; }
    }

    /// <summary>
    ///     This interface gives components behavior when being used to "attack".
    /// </summary>
    public interface IAttack
    {
        void Attack(AttackEventArgs eventArgs);
    }

    public class AttackEventArgs : EventArgs
    {
        public AttackEventArgs(IEntity user, GridCoordinates clickLocation)
        {
            User = user;
            ClickLocation = clickLocation;
        }

        public IEntity User { get; }
        public GridCoordinates ClickLocation { get; }
    }

    /// <summary>
    /// Governs interactions during clicking on entities
    /// </summary>
    [UsedImplicitly]
    public sealed class InteractionSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
#pragma warning restore 649

        public const float InteractionRange = 2;
        public const float InteractionRangeSquared = InteractionRange * InteractionRange;

        public override void Initialize()
        {
            var inputSys = EntitySystemManager.GetEntitySystem<InputSystem>();
            inputSys.BindMap.BindFunction(ContentKeyFunctions.UseItemInHand,
                new PointerInputCmdHandler(HandleUseItemInHand));
            inputSys.BindMap.BindFunction(ContentKeyFunctions.ActivateItemInWorld,
                new PointerInputCmdHandler(HandleActivateItemInWorld));
        }

        public void HandleActivateItemInWorld(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            if (!EntityManager.TryGetEntity(uid, out var used))
                return;

            var playerEnt = ((IPlayerSession) session).AttachedEntity;

            if (playerEnt == null || !playerEnt.IsValid())
            {
                return;
            }

            if (!playerEnt.Transform.GridPosition.InRange(_mapManager, used.Transform.GridPosition, InteractionRange))
            {
                return;
            }

            InteractionActivate(playerEnt, used);
        }

        private void InteractionActivate(IEntity user, IEntity used)
        {
            var activateMsg = new ActivateInWorldMessage(user, used);
            RaiseEvent(activateMsg);
            if (activateMsg.Handled)
            {
                return;
            }

            if (!used.TryGetComponent(out IActivate activateComp))
            {
                return;
            }

            activateComp.Activate(new ActivateEventArgs {User = user});
        }

        private void HandleUseItemInHand(ICommonSession session, GridCoordinates coords, EntityUid uid)
        {
            // client sanitization
            if (!_mapManager.GridExists(coords.GridID))
            {
                Logger.InfoS("system.interaction", $"Invalid Coordinates: client={session}, coords={coords}");
                return;
            }

            if (uid.IsClientSide())
            {
                Logger.WarningS("system.interaction",
                    $"Client sent interaction with client-side entity. Session={session}, Uid={uid}");
                return;
            }

            var userEntity = ((IPlayerSession) session).AttachedEntity;

            if (userEntity.TryGetComponent(out CombatModeComponent combatMode) && combatMode.IsInCombatMode)
            {
                DoAttack(userEntity, coords, uid);
            }
            else
            {
                UserInteraction(userEntity, coords, uid);
            }
        }

        private void UserInteraction(IEntity player, GridCoordinates coordinates, EntityUid clickedUid)
        {
            // Get entity clicked upon from UID if valid UID, if not assume no entity clicked upon and null
            if (!EntityManager.TryGetEntity(clickedUid, out var attacked))
            {
                attacked = null;
            }

            // Verify player has a transform component
            if (!player.TryGetComponent<ITransformComponent>(out var playerTransform))
            {
                return;
            }

            // Verify player is on the same map as the entity he clicked on
            if (_mapManager.GetGrid(coordinates.GridID).ParentMap.Index != playerTransform.MapID)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on a map he isn't located on");
                return;
            }

            // Verify player has a hand, and find what object he is currently holding in his active hand
            if (!player.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var item = hands.GetActiveHand?.Owner;

            if (!ActionBlockerSystem.CanInteract(player))
            {
                return;
            }

            // TODO: Check if client should be able to see that object to click on it in the first place

            // Clicked on empty space behavior, try using ranged attack
            if (attacked == null)
            {
                if (item != null)
                {
                    // After attack: Check if we clicked on an empty location, if so the only interaction we can do is AfterAttack
                    InteractAfterAttack(player, item, coordinates);
                }

                return;
            }

            // Verify attacked object is on the map if we managed to click on it somehow
            if (!attacked.Transform.IsMapTransform)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on object {attacked.Name} that isn't currently on the map somehow");
                return;
            }

            // Check if ClickLocation is in object bounds here, if not lets log as warning and see why
            if (attacked.TryGetComponent(out BoundingBoxComponent boundingBox))
            {
                if (!boundingBox.WorldAABB.Contains(coordinates.Position))
                {
                    Logger.WarningS("system.interaction",
                        $"Player {player.Name} clicked {attacked.Name} outside of its bounding box component somehow");
                    return;
                }
            }

            // RangedAttack/AfterAttack: Check distance between user and clicked item, if too large parse it in the ranged function
            // TODO: have range based upon the item being used? or base it upon some variables of the player himself?
            var distance = (playerTransform.WorldPosition - attacked.Transform.WorldPosition).LengthSquared;
            if (distance > InteractionRangeSquared)
            {
                if (item != null)
                {
                    RangedInteraction(player, item, attacked, coordinates);
                    return;
                }

                return; // Add some form of ranged AttackHand here if you need it someday, or perhaps just ways to modify the range of AttackHand
            }

            // We are close to the nearby object and the object isn't contained in our active hand
            // AttackBy/AfterAttack: We will either use the item on the nearby object
            if (item != null)
            {
                Interaction(player, item, attacked, coordinates);
            }
            // AttackHand/Activate: Since our hand is empty we will use AttackHand/Activate
            else
            {
                Interaction(player, attacked);
            }
        }

        /// <summary>
        ///     We didn't click on any entity, try doing an AfterAttack on the click location
        /// </summary>
        private void InteractAfterAttack(IEntity user, IEntity weapon, GridCoordinates clickLocation)
        {
            var message = new AfterAttackMessage(user, weapon, null, clickLocation);
            RaiseEvent(message);
            if (message.Handled)
            {
                return;
            }

            var afterAttacks = weapon.GetAllComponents<IAfterAttack>().ToList();
            var afterAttackEventArgs = new AfterAttackEventArgs {User = user, ClickLocation = clickLocation};

            foreach (var afterAttack in afterAttacks)
            {
                afterAttack.AfterAttack(afterAttackEventArgs);
            }
        }

        /// <summary>
        /// Uses a weapon/object on an entity
        /// Finds components with the AttackBy interface and calls their function
        /// </summary>
        public void Interaction(IEntity user, IEntity weapon, IEntity attacked, GridCoordinates clickLocation)
        {
            var attackMsg = new AttackByMessage(user, weapon, attacked, clickLocation);
            RaiseEvent(attackMsg);
            if (attackMsg.Handled)
            {
                return;
            }

            var attackBys = attacked.GetAllComponents<IAttackBy>().ToList();
            var attackByEventArgs = new AttackByEventArgs
            {
                User = user, ClickLocation = clickLocation, AttackWith = weapon
            };

            foreach (var attackBy in attackBys)
            {
                if (attackBy.AttackBy(attackByEventArgs))
                {
                    // If an AttackBy returns a status completion we finish our attack
                    return;
                }
            }

            var afterAtkMsg = new AfterAttackMessage(user, weapon, attacked, clickLocation);
            RaiseEvent(afterAtkMsg);
            if (afterAtkMsg.Handled)
            {
                return;
            }

            // If we aren't directly attacking the nearby object, lets see if our item has an after attack we can do
            var afterAttacks = weapon.GetAllComponents<IAfterAttack>().ToList();
            var afterAttackEventArgs = new AfterAttackEventArgs
            {
                User = user, ClickLocation = clickLocation, Attacked = attacked
            };

            foreach (var afterAttack in afterAttacks)
            {
                afterAttack.AfterAttack(afterAttackEventArgs);
            }
        }

        /// <summary>
        /// Uses an empty hand on an entity
        /// Finds components with the AttackHand interface and calls their function
        /// </summary>
        public void Interaction(IEntity user, IEntity attacked)
        {
            var message = new AttackHandMessage(user, attacked);
            RaiseEvent(message);
            if (message.Handled)
            {
                return;
            }

            var attackHands = attacked.GetAllComponents<IAttackHand>().ToList();
            var attackHandEventArgs = new AttackHandEventArgs {User = user};

            foreach (var attackHand in attackHands)
            {
                if (attackHand.AttackHand(attackHandEventArgs))
                {
                    // If an AttackHand returns a status completion we finish our attack
                    return;
                }
            }

            // Else we run Activate.
            InteractionActivate(user, attacked);
        }

        /// <summary>
        /// Activates the Use behavior of an object
        /// Verifies that the user is capable of doing the use interaction first
        /// </summary>
        /// <param name="user"></param>
        /// <param name="used"></param>
        public void TryUseInteraction(IEntity user, IEntity used)
        {
            if (user != null && used != null && ActionBlockerSystem.CanUse(user))
            {
                UseInteraction(user, used);
            }
        }

        /// <summary>
        /// Activates/Uses an object in control/possession of a user
        /// If the item has the IUse interface on one of its components we use the object in our hand
        /// </summary>
        public void UseInteraction(IEntity user, IEntity used)
        {
            var useMsg = new UseInHandMessage(user, used);
            RaiseEvent(useMsg);
            if (useMsg.Handled)
            {
                return;
            }

            var uses = used.GetAllComponents<IUse>().ToList();

            // Try to use item on any components which have the interface
            foreach (var use in uses)
            {
                if (use.UseEntity(new UseEntityEventArgs {User = user}))
                {
                    // If a Use returns a status completion we finish our attack
                    return;
                }
            }
        }

        /// <summary>
        /// Activates the Use behavior of an object
        /// Verifies that the user is capable of doing the use interaction first
        /// </summary>
        public bool TryThrowInteraction(IEntity user, IEntity item)
        {
            if (user == null || item == null || !ActionBlockerSystem.CanThrow(user)) return false;

            ThrownInteraction(user, item);
            return true;

        }

        /// <summary>
        ///     Calls Thrown on all components that implement the IThrown interface
        ///     on an entity that has been thrown.
        /// </summary>
        public void ThrownInteraction(IEntity user, IEntity thrown)
        {
            var throwMsg = new ThrownMessage(user, thrown);
            RaiseEvent(throwMsg);
            if (throwMsg.Handled)
            {
                return;
            }

            var comps = thrown.GetAllComponents<IThrown>().ToList();

            // Call Thrown on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Thrown(new ThrownEventArgs(user));
            }
        }

        /// <summary>
        ///     Calls Land on all components that implement the ILand interface
        ///     on an entity that has landed after being thrown.
        /// </summary>
        public void LandInteraction(IEntity user, IEntity landing, GridCoordinates landLocation)
        {
            var landMsg = new LandMessage(user, landing, landLocation);
            RaiseEvent(landMsg);
            if (landMsg.Handled)
            {
                return;
            }

            var comps = landing.GetAllComponents<ILand>().ToList();

            // Call Land on all components that implement the interface
            foreach (var comp in comps)
            {
                comp.Land(new LandEventArgs(user, landLocation));
            }
        }

        /// <summary>
        /// Will have two behaviors, either "uses" the weapon at range on the entity if it is capable of accepting that action
        /// Or it will use the weapon itself on the position clicked, regardless of what was there
        /// </summary>
        public void RangedInteraction(IEntity user, IEntity weapon, IEntity attacked, GridCoordinates clickLocation)
        {
            var rangedMsg = new RangedAttackMessage(user, weapon, attacked, clickLocation);
            RaiseEvent(rangedMsg);
            if (rangedMsg.Handled)
                return;

            var rangedAttackBys = attacked.GetAllComponents<IRangedAttackBy>().ToList();
            var rangedAttackByEventArgs = new RangedAttackByEventArgs
            {
                User = user, Weapon = weapon, ClickLocation = clickLocation
            };

            // See if we have a ranged attack interaction
            foreach (var t in rangedAttackBys)
            {
                if (t.RangedAttackBy(rangedAttackByEventArgs))
                {
                    // If an AttackBy returns a status completion we finish our attack
                    return;
                }
            }

            var afterAtkMsg = new AfterAttackMessage(user, weapon, attacked, clickLocation);
            RaiseEvent(afterAtkMsg);
            if (afterAtkMsg.Handled)
                return;

            var afterAttacks = weapon.GetAllComponents<IAfterAttack>().ToList();
            var afterAttackEventArgs = new AfterAttackEventArgs
            {
                User = user, ClickLocation = clickLocation, Attacked = attacked
            };

            //See if we have a ranged attack interaction
            foreach (var afterAttack in afterAttacks)
            {
                afterAttack.AfterAttack(afterAttackEventArgs);
            }
        }

        private void DoAttack(IEntity player, GridCoordinates coordinates, EntityUid uid)
        {
            // Verify player is on the same map as the entity he clicked on
            if (_mapManager.GetGrid(coordinates.GridID).ParentMap.Index != player.Transform.MapID)
            {
                Logger.WarningS("system.interaction",
                    $"Player named {player.Name} clicked on a map he isn't located on");
                return;
            }

            // Verify player has a hand, and find what object he is currently holding in his active hand
            if (!player.TryGetComponent<IHandsComponent>(out var hands))
            {
                return;
            }

            var item = hands.GetActiveHand?.Owner;

            // TODO: If item is null we need some kinda unarmed combat.
            if (!ActionBlockerSystem.CanInteract(player) || item == null)
            {
                return;
            }

            var eventArgs = new AttackEventArgs(player, coordinates);
            foreach (var attackComponent in item.GetAllComponents<IAttack>())
            {
                attackComponent.Attack(eventArgs);
            }
        }
    }

    /// <summary>
    ///     Raised when being clicked on or "attacked" by a user with an object in their hand
    /// </summary>
    [PublicAPI]
    public class AttackByMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that the User attacked with.
        /// </summary>
        public IEntity ItemInHand { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity Attacked { get; }

        /// <summary>
        ///     The original location that was clicked by the user.
        /// </summary>
        public GridCoordinates ClickLocation { get; }

        public AttackByMessage(IEntity user, IEntity itemInHand, IEntity attacked, GridCoordinates clickLocation)
        {
            User = user;
            ItemInHand = itemInHand;
            Attacked = attacked;
            ClickLocation = clickLocation;
        }
    }

    /// <summary>
    ///      Raised when being clicked on or "attacked" by a user with an empty hand.
    /// </summary>
    [PublicAPI]
    public class AttackHandMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity Attacked { get; }

        public AttackHandMessage(IEntity user, IEntity attacked)
        {
            User = user;
            Attacked = attacked;
        }
    }

    /// <summary>
    ///     Raised when being clicked by objects outside the range of direct use.
    /// </summary>
    [PublicAPI]
    public class RangedAttackMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that the User attacked with.
        /// </summary>
        public IEntity ItemInHand { get; set; }

        /// <summary>
        ///     Entity that was attacked.
        /// </summary>
        public IEntity Attacked { get; }

        /// <summary>
        ///     Location that the user clicked outside of their interaction range.
        /// </summary>
        public GridCoordinates ClickLocation { get; }

        public RangedAttackMessage(IEntity user, IEntity itemInHand, IEntity attacked, GridCoordinates clickLocation)
        {
            User = user;
            ItemInHand = itemInHand;
            ClickLocation = clickLocation;
            Attacked = attacked;
        }
    }

    /// <summary>
    ///     Raised when clicking on another object and no attack event was handled.
    /// </summary>
    [PublicAPI]
    public class AfterAttackMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that triggered the attack.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that the User attacked with.
        /// </summary>
        public IEntity ItemInHand { get; set; }

        /// <summary>
        ///     Entity that was attacked. This can be null if the attack did not click on an entity.
        /// </summary>
        public IEntity Attacked { get; }

        /// <summary>
        ///     Location that the user clicked outside of their interaction range.
        /// </summary>
        public GridCoordinates ClickLocation { get; }

        public AfterAttackMessage(IEntity user, IEntity itemInHand, IEntity attacked, GridCoordinates clickLocation)
        {
            User = user;
            Attacked = attacked;
            ClickLocation = clickLocation;
            ItemInHand = itemInHand;
        }
    }

    /// <summary>
    ///     Raised when using the entity in your hands.
    /// </summary>
    [PublicAPI]
    public class UseInHandMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity holding the item in their hand.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was used.
        /// </summary>
        public IEntity Used { get; }

        public UseInHandMessage(IEntity user, IEntity used)
        {
            User = user;
            Used = used;
        }
    }

    /// <summary>
    ///     Raised when throwing the entity in your hands.
    /// </summary>
    [PublicAPI]
    public class ThrownMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that threw the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was thrown.
        /// </summary>
        public IEntity Thrown { get; }

        public ThrownMessage(IEntity user, IEntity thrown)
        {
            User = user;
            Thrown = thrown;
        }
    }

    /// <summary>
    ///     Raised when an entity that was thrown lands.
    /// </summary>
    [PublicAPI]
    public class LandMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that threw the item.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Item that was thrown.
        /// </summary>
        public IEntity Thrown { get; }

        /// <summary>
        ///     Location where the item landed.
        /// </summary>
        public GridCoordinates LandLocation { get; }

        public LandMessage(IEntity user, IEntity thrown, GridCoordinates landLocation)
        {
            User = user;
            Thrown = thrown;
            LandLocation = landLocation;
        }
    }

    /// <summary>
    ///     Raised when an entity is activated in the world.
    /// </summary>
    [PublicAPI]
    public class ActivateInWorldMessage : EntitySystemMessage
    {
        /// <summary>
        ///     If this message has already been "handled" by a previous system.
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        ///     Entity that activated the world entity.
        /// </summary>
        public IEntity User { get; }

        /// <summary>
        ///     Entity that was activated in the world.
        /// </summary>
        public IEntity Activated { get; }

        public ActivateInWorldMessage(IEntity user, IEntity activated)
        {
            User = user;
            Activated = activated;
        }
    }
}
