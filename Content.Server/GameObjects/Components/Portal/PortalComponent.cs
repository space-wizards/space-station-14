#nullable enable
using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Portal;
using Content.Shared.GameObjects.Components.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Player;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Portal
{
    [RegisterComponent]
    public class PortalComponent : SharedPortalComponent, IStartCollide
    {
        // Potential improvements: Different sounds,
        // Add Gateways
        // More efficient form of GetEntitiesIntersecting,
        // Put portal above most other things layer-wise
        // Add telefragging (get entities on connecting portal and force brute damage)

        private IEntity? _connectingTeleporter;
        private PortalState _state = PortalState.Pending;
        [ViewVariables(VVAccess.ReadWrite)] [DataField("individual_cooldown")] private float _individualPortalCooldown = 2.1f;
        [ViewVariables] [DataField("overall_cooldown")] private float _overallPortalCooldown = 2.0f;
        [ViewVariables] private bool _onCooldown;
        [ViewVariables] [DataField("departure_sound")] private string _departureSound = "/Audio/Effects/teleport_departure.ogg";
        [ViewVariables] [DataField("arrival_sound")] private string _arrivalSound = "/Audio/Effects/teleport_arrival.ogg";
        public readonly List<IEntity> ImmuneEntities = new(); // K
        [ViewVariables(VVAccess.ReadWrite)] [DataField("alive_time")] private float _aliveTime = 10f;

        public override void OnAdd()
        {
            // This will blow up an entity it's attached to
            base.OnAdd();

            _state = PortalState.Pending;

            if (_aliveTime > 0)
            {
                Owner.SpawnTimer(TimeSpan.FromSeconds(_aliveTime), () => Owner.Delete());
            }
        }

        public bool CanBeConnected()
        {
            return _connectingTeleporter == null;
        }

        public void TryConnectPortal(IEntity otherPortal)
        {
            if (otherPortal.TryGetComponent<PortalComponent>(out var connectedPortal) && connectedPortal.CanBeConnected())
            {
                _connectingTeleporter = otherPortal;
                connectedPortal._connectingTeleporter = Owner;
                TryChangeState(PortalState.Pending);
            }
        }

        public void TryChangeState(PortalState targetState)
        {
            if (Deleted)
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
            if (Deleted)
            {
                return;
            }

            if (ImmuneEntities.Contains(entity))
            {
                ImmuneEntities.Remove(entity);
            }

            if (_connectingTeleporter != null &&
                _connectingTeleporter.TryGetComponent<PortalComponent>(out var otherPortal))
            {
                otherPortal.ImmuneEntities.Remove(entity);
            }
        }

        private bool IsEntityPortable(IEntity entity)
        {
            // TODO: Check if it's slotted etc. Otherwise the slot item itself gets ported.
            return !ImmuneEntities.Contains(entity) &&
                   entity.HasTag("Teleportable");
        }

        public void StartCooldown()
        {
            if (_overallPortalCooldown <= 0 || _onCooldown)
            {
                // Just in case?
                _onCooldown = false;
                return;
            }

            _onCooldown = true;
            TryChangeState(PortalState.RecentlyTeleported);

            if (_connectingTeleporter == null ||
                !_connectingTeleporter.TryGetComponent<PortalComponent>(out var otherPortal))
            {
                return;
            }

            otherPortal.TryChangeState(PortalState.RecentlyTeleported);

            Owner.SpawnTimer(TimeSpan.FromSeconds(_overallPortalCooldown), () =>
            {
                _onCooldown = false;
                TryChangeState(PortalState.Pending);
                otherPortal.TryChangeState(PortalState.Pending);
            });
        }

        public void TryPortalEntity(IEntity entity)
        {
            if (ImmuneEntities.Contains(entity) ||
                _connectingTeleporter == null ||
                !IsEntityPortable(entity))
            {
                return;
            }

            var position = _connectingTeleporter.Transform.Coordinates;

            // Departure
            // Do we need to rate-limit sounds to stop ear BLAST?
            SoundSystem.Play(Filter.Pvs(entity), _departureSound, entity.Transform.Coordinates);
            entity.Transform.Coordinates = position;
            SoundSystem.Play(Filter.Pvs(entity), _arrivalSound, entity.Transform.Coordinates);
            TryChangeState(PortalState.RecentlyTeleported);

            // To stop spam teleporting. Could potentially look at adding a timer to flush this from the portal
            ImmuneEntities.Add(entity);
            _connectingTeleporter.GetComponent<PortalComponent>().ImmuneEntities.Add(entity);
            Owner.SpawnTimer(TimeSpan.FromSeconds(_individualPortalCooldown), () => ReleaseCooldown(entity));
            StartCooldown();
        }

        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
            if (_onCooldown == false)
            {
                TryPortalEntity(otherFixture.Body.Owner);
            }
        }
    }
}
