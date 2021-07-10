#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Interaction;
using Content.Shared.Portal.Components;
using Content.Shared.Sound;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Portal.Components
{
    [RegisterComponent]
    public class TeleporterComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;
        [Dependency] private readonly IRobustRandom _spreadRandom = default!;

        // TODO: Look at MapManager.Map for Beacons to get all entities on grid
        public ItemTeleporterState State => _state;

        public override string Name => "ItemTeleporter";

        [DataField("charge_time")]
        [ViewVariables] private float _chargeTime = 0.2f;
        [DataField("cooldown")]
        [ViewVariables] private float _cooldown = 2f;
        [DataField("range")]
        [ViewVariables] private int _range = 15;
        [ViewVariables] private ItemTeleporterState _state;
        [DataField("teleporter_type")]
        [ViewVariables] private TeleporterType _teleporterType = TeleporterType.Random;
        [ViewVariables] [DataField("departure_sound")] private SoundSpecifier _departureSound = new SoundPathSpecifier("/Audio/Effects/teleport_departure.ogg");
        [ViewVariables] [DataField("arrival_sound")] private SoundSpecifier _arrivalSound = new SoundPathSpecifier("/Audio/Effects/teleport_arrival.ogg");
        [ViewVariables] [DataField("cooldown_sound")] private SoundSpecifier _cooldownSound = default!;
        // If the direct OR random teleport will try to avoid hitting collidables
        [DataField("avoid_walls")] [ViewVariables]
        private bool _avoidCollidable = true;
        [DataField("portal_alive_time")]
        [ViewVariables] private float _portalAliveTime = 5f;

        private void SetState(ItemTeleporterState newState)
        {
            if (!Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                return;
            }

            if (newState == ItemTeleporterState.Cooldown)
            {
                appearance.SetData(TeleporterVisuals.VisualState, TeleporterVisualState.Charging);
            }
            else
            {
                appearance.SetData(TeleporterVisuals.VisualState, TeleporterVisualState.Ready);
            }
            _state = newState;
        }

        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (_teleporterType == TeleporterType.Directed)
            {
                TryDirectedTeleport(eventArgs.User, eventArgs.ClickLocation.ToMap(Owner.EntityManager));
            }

            if (_teleporterType == TeleporterType.Random)
            {
                TryRandomTeleport(eventArgs.User);
            }

            return true;
        }

        public void TryDirectedTeleport(IEntity user, MapCoordinates mapCoords)
        {
            // Checks
            if ((user.Transform.WorldPosition - mapCoords.Position).LengthSquared > _range * _range)
            {
                return;
            }

            if (_state == ItemTeleporterState.On)
            {
                return;
            }
            if (_avoidCollidable)
            {
                foreach (var entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesIntersecting(mapCoords))
                {
                    // Added this component to avoid stacking portals and causing shenanigans
                    // TODO: Doesn't do a great job of stopping stacking portals for directed
                    if (entity.HasComponent<IPhysBody>() || entity.HasComponent<TeleporterComponent>())
                    {
                        return;
                    }
                }
            }
            // Start / Continue
            if (_state == ItemTeleporterState.Off)
            {
                SetState(ItemTeleporterState.Charging);
                // Play charging sound here if you want
            }

            if (_state != ItemTeleporterState.Charging)
            {
                return;
            }

            Owner.SpawnTimer(TimeSpan.FromSeconds(_chargeTime), () => Teleport(user, mapCoords.Position));
            StartCooldown();
        }

        public void StartCooldown()
        {
            SetState(ItemTeleporterState.Cooldown);
            Owner.SpawnTimer(TimeSpan.FromSeconds(_chargeTime + _cooldown), () => SetState(ItemTeleporterState.Off));
            if (_cooldownSound.TryGetSound(out var cooldownSound))
            {
                SoundSystem.Play(Filter.Pvs(Owner), cooldownSound, Owner);
            }
        }

        protected override void Initialize()
        {
            base.Initialize();
            _state = ItemTeleporterState.Off;
        }

        private bool EmptySpace(IEntity user, Vector2 target)
        {
            // TODO: Check the user's spot? Upside is no stacking TPs but downside is they can't unstuck themselves from walls.
            foreach (var entity in IoCManager.Resolve<IEntityLookup>().GetEntitiesIntersecting(user.Transform.MapID, target))
            {
                if (entity.HasComponent<IPhysBody>() || entity.HasComponent<PortalComponent>())
                {
                    return false;
                }
            }
            return true;
        }

        private Vector2 RandomEmptySpot(IEntity user, int range)
        {
            Vector2 targetVector = user.Transform.Coordinates.Position;
            // Definitely a better way to do this
            foreach (var i in Enumerable.Range(0, 5))
            {
                var randomRange = _spreadRandom.Next(0, range);
                var angle = Angle.FromDegrees(_spreadRandom.Next(0, 359));
                targetVector = user.Transform.Coordinates.Position + angle.ToVec() * randomRange;
                if (EmptySpace(user, targetVector))
                {
                    return targetVector;
                }
                if (i == 19)
                {
                    return targetVector;
                }
            }

            return targetVector;
        }

        public void TryRandomTeleport(IEntity user)
        {
            // Checks
            if (_state == ItemTeleporterState.On)
            {
                return;
            }

            Vector2 targetVector;
            if (_avoidCollidable)
            {
                targetVector = RandomEmptySpot(user, _range);
            }
            else
            {
               var randomRange = _spreadRandom.Next(0, _range);
               var angle = Angle.FromDegrees(_spreadRandom.Next(0, 359));
               targetVector = user.Transform.Coordinates.Position + angle.ToVec() * randomRange;
            }
            // Start / Continue
            if (_state == ItemTeleporterState.Off)
            {
                SetState(ItemTeleporterState.Charging);
            }

            if (_state != ItemTeleporterState.Charging)
            {
                return;
            }

            // Seemed easier to just start the cd timer at the same time
            Owner.SpawnTimer(TimeSpan.FromSeconds(_chargeTime), () => Teleport(user, targetVector));
            StartCooldown();
        }

        public void Teleport(IEntity user, Vector2 vector)
        {
            // Messy maybe?
            var targetGrid = user.Transform.Coordinates.WithPosition(vector);

            // If portals use those, otherwise just move em over
            if (_portalAliveTime > 0.0f)
            {
                // Call Delete here as the teleporter should have control over portal longevity
                // Departure portal
                var departurePortal = _serverEntityManager.SpawnEntity("Portal", user.Transform.Coordinates);

                // Arrival portal
                var arrivalPortal = _serverEntityManager.SpawnEntity("Portal", targetGrid);
                if (arrivalPortal.TryGetComponent<PortalComponent>(out var arrivalComponent))
                {
                    // Connect.
                    arrivalComponent.TryConnectPortal(departurePortal);
                }
            }
            else
            {
                // Departure
                if(_departureSound.TryGetSound(out var departureSound))
                    SoundSystem.Play(Filter.Pvs(user), departureSound, user.Transform.Coordinates);

                // Arrival
                user.Transform.AttachToGridOrMap();
                user.Transform.WorldPosition = vector;
                if(_arrivalSound.TryGetSound(out var arrivalSound))
                    SoundSystem.Play(Filter.Pvs(user), arrivalSound, user.Transform.Coordinates);
            }
        }
    }
}
