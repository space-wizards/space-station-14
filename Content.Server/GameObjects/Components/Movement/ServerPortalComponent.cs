#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Movement;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Timers;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Movement
{
    [RegisterComponent]
    public class ServerPortalComponent : SharedPortalComponent
    {
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;

        // Potential improvements: Different sounds,
        // Add Gateways
        // More efficient form of GetEntitiesIntersecting,
        // Put portal above most other things layer-wise
        // Add telefragging (get entities on connecting portal and force brute damage)

        private IEntity? _connectingTeleporter;
        private PortalState _state = PortalState.Pending;
        [ViewVariables(VVAccess.ReadWrite)] private float _individualPortalCooldown;
        [ViewVariables] private float _overallPortalCooldown;
        [ViewVariables] private bool _onCooldown;
        [ViewVariables] private string _departureSound = "";
        [ViewVariables] private string _arrivalSound = "";
        public readonly List<IEntity> ImmuneEntities = new List<IEntity>(); // K
        [ViewVariables(VVAccess.ReadWrite)] private float _aliveTime;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            // How long will the portal stay up: 0 is infinite
            serializer.DataField(ref _aliveTime, "alive_time", 10.0f);
            // How long before a specific person can go back into it
            serializer.DataField(ref _individualPortalCooldown, "individual_cooldown", 2.1f);
            // How long before anyone can go in it
            serializer.DataField(ref _overallPortalCooldown, "overall_cooldown", 2.0f);
            serializer.DataField(ref _departureSound, "departure_sound", "/Audio/Effects/teleport_departure.ogg");
            serializer.DataField(ref _arrivalSound, "arrival_sound", "/Audio/Effects/teleport_arrival.ogg");
        }

        public override void OnAdd()
        {
            // This will blow up an entity it's attached to
            base.OnAdd();
            if (Owner.TryGetComponent<ICollidableComponent>(out var collide))
            {
                //collide.IsHardCollidable = false;
            }

            _state = PortalState.Pending;
            if (_aliveTime > 0)
            {
                Timer.Spawn(TimeSpan.FromSeconds(_aliveTime), () => Owner.Delete());
            }
        }

        public bool CanBeConnected()
        {
            if (_connectingTeleporter == null)
            {
                return true;
            }
            return false;
        }

        public void TryConnectPortal(IEntity otherPortal)
        {
            if (otherPortal.TryGetComponent<ServerPortalComponent>(out var connectedPortal) && connectedPortal.CanBeConnected())
            {
                _connectingTeleporter = otherPortal;
                connectedPortal._connectingTeleporter = Owner;
                TryChangeState(PortalState.Pending);
            }
        }

        public void TryChangeState(PortalState targetState)
        {
            if (Owner == null)
            {
                return;
            }

            _state = targetState;

            if (Owner.TryGetComponent(out AppearanceComponent? appearance))
            {
                appearance.SetData(PortalVisuals.State, _state);
            }
        }

        private void ReleaseCooldown(IEntity entity)
        {
            if (ImmuneEntities.Contains(entity))
            {
                ImmuneEntities.Remove(entity);
            }

            if (_connectingTeleporter != null &&
                _connectingTeleporter.TryGetComponent<ServerPortalComponent>(out var otherPortal))
            {
                otherPortal.ImmuneEntities.Remove(entity);
            }
        }

        public IEnumerable<IEntity> GetPortableEntities()
        {
            foreach (var entity in _serverEntityManager.GetEntitiesIntersecting(Owner))
            {
                if (IsEntityPortable(entity))
                {
                    yield return entity;
                }
            }
        }

        private bool IsEntityPortable(IEntity entity)
        {
            // TODO: Check if it's slotted etc. Otherwise the slot item itself gets ported.
            if (!ImmuneEntities.Contains(entity) && entity.HasComponent<TeleportableComponent>())
            {
                return true;
            }
            return false;
        }

        // TODO: Fix portal updates for performance
        public void OnUpdate()
        {
            if (_onCooldown == false)
            {
                foreach (var entity in GetPortableEntities())
                {
                    TryPortalEntity(entity);
                    break;
                }
            }
        }

        public void StartCooldown()
        {
            if (_overallPortalCooldown > 0 && _onCooldown == false)
            {
                _onCooldown = true;
                TryChangeState(PortalState.RecentlyTeleported);
                if (_connectingTeleporter != null)
                {
                    _connectingTeleporter.TryGetComponent<ServerPortalComponent>(out var otherPortal);
                    if (otherPortal != null)
                    {
                        otherPortal.TryChangeState(PortalState.RecentlyTeleported);
                        Timer.Spawn(TimeSpan.FromSeconds(_overallPortalCooldown), () =>
                        {
                            _onCooldown = false;
                            TryChangeState(PortalState.Pending);
                            otherPortal.TryChangeState(PortalState.Pending);
                        });
                    }
                }
            }
            else
            {
                // Just in case?
                _onCooldown = false;
            }
        }

        public void TryPortalEntity(IEntity entity)
        {
            if (ImmuneEntities.Contains(entity) || _connectingTeleporter == null)
            {
                return;
            }

            var position = _connectingTeleporter.Transform.Coordinates;
            var soundPlayer = EntitySystem.Get<AudioSystem>();

            // Departure
            // Do we need to rate-limit sounds to stop ear BLAST?
            soundPlayer.PlayAtCoords(_departureSound, entity.Transform.Coordinates);
            entity.Transform.AttachToGridOrMap();
            entity.Transform.Coordinates = position;
            soundPlayer.PlayAtCoords(_arrivalSound, entity.Transform.Coordinates);
            TryChangeState(PortalState.RecentlyTeleported);
            // To stop spam teleporting. Could potentially look at adding a timer to flush this from the portal
            ImmuneEntities.Add(entity);
            _connectingTeleporter.GetComponent<ServerPortalComponent>().ImmuneEntities.Add(entity);
            Timer.Spawn(TimeSpan.FromSeconds(_individualPortalCooldown), () => ReleaseCooldown(entity));
            StartCooldown();
        }
    }
}
