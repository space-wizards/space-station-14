#nullable enable
using Content.Server.Atmos;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Shared.Atmos;
using Content.Shared.GameObjects.EntitySystems.ActionBlocker;
using Content.Shared.GameObjects.Verbs;
using Robust.Server.GameObjects.Components.Container;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.ComponentDependencies;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Content.Server.GameObjects.Components.Disposal
{
    //TODO: Implement outgoing messages
    //TODO: Write documentation
    /// <summary>
    /// 
    /// </summary>
    [RegisterComponent]
    public class DisposalInserterComponent : Component, IGasMixtureHolder
    {
        [ComponentDependency]
        public readonly PowerReceiverComponent? PowerReceiver = null;

        public override string Name => "DisposalInserter";

        [ViewVariables]
        public bool Anchored => !Owner.TryGetComponent(out IPhysicsComponent? physics) || physics.Anchored;

        /// <summary>
        /// Used by the disposal entry component to get the entities from the inserter
        /// </summary>
        [ViewVariables]
        public IReadOnlyList<IEntity> ContainedEntities => _container.ContainedEntities;

        [ViewVariables]
        public bool Powered => PowerReceiver != null && PowerReceiver.Powered;

        [ViewVariables]
        public GasMixture Air { get; set; } = default!;

        public bool Engaged { get; set; }

        /// <summary>
        ///     The engage pressure of this inserter.
        ///     Prevents it from flushing if the air pressure inside it is not equal to or bigger than this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _engagePressure;

        /// <summary>
        /// The time it takes until the inserter engages itself after an entity was inserted
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private TimeSpan _automaticEngageTime;

        [ViewVariables(VVAccess.ReadWrite)]
        private TimeSpan _flushDelay;

        /// <summary>
        ///     Token used to cancel the automatic engage of a disposal unit
        ///     after an entity enters it.
        /// </summary>
        private CancellationTokenSource? _automaticEngageToken;

        private string _containerID = "";

        /// <summary>
        ///     Container of entities inside this disposal unit.
        /// </summary>
        [ViewVariables]
        private Container _container = default!;

        //[ViewVariables]
        //private PressureState State => _pressure >= 1 ? PressureState.Ready : PressureState.Pressurizing;

        //Init stuff

        /// <summary>
        /// 
        /// </summary>
        public void Update(float frameTime)
        {
            if (!Powered || Air.Pressure < _engagePressure && !Pressurize(frameTime))
            {
                return;
            }

            if(Air.Pressure >= _engagePressure)
            {
                if (Engaged)
                {
                    TryFlush();
                }
            }
            else
            {
                SendMessage(new PressureChangedMessage(Air.Pressure));
            }
        }

        protected override void Startup()
        {
            base.Startup();

            if (_container == default!)
            {
                _container = ContainerManagerComponent.Ensure<Container>(_containerID, Owner);
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction(
                "engagePressure",
                101.0f,
                engagePressure => _engagePressure = engagePressure,
                () => _engagePressure);

            serializer.DataReadWriteFunction(
                "automaticEngageTime",
                30,
                seconds => _automaticEngageTime = TimeSpan.FromSeconds(seconds),
                () => (int) _automaticEngageTime.TotalSeconds);

            serializer.DataReadWriteFunction(
                "flushDelay",
                3,
                seconds => _flushDelay = TimeSpan.FromSeconds(seconds),
                () => (int) _flushDelay.TotalSeconds);

            serializer.DataField(this, x => x.Air, "air", new GasMixture(Atmospherics.CellVolume / 2));
            serializer.DataField(ref _containerID, "containerName", Name);
        }

        public bool TryFlush()
        {
            if (!CanFlush() || !Owner.TryGetComponent(out SnapGridComponent? snapGrid))
            {
                return false;
            }

            var entry = snapGrid.GetLocal().FirstOrDefault(entity => entity.HasComponent<DisposalEntryComponent>());

            if (entry == null)
            {
                return false;
            }

            SendMessage(new InserterFlushedMessage());

            var entryComponent = entry.GetComponent<DisposalEntryComponent>();

            //entryComponent.TryInsert(this);

            _automaticEngageToken?.Cancel();
            _automaticEngageToken = null;

            Engaged = false;

            return true;
        }

        public override void OnRemove()
        {
            foreach (var entity in _container.ContainedEntities.ToArray())
            {
                _container.ForceRemove(entity);
            }

            _automaticEngageToken?.Cancel();
            _automaticEngageToken = null;

            _container = null!;

            base.OnRemove();
        }

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);

            switch (message)
            {
                case PowerChangedMessage powerChanged:
                    PowerStateChanged(powerChanged);
                    break;

                case ContainerContentsModifiedMessage contentsModified:
                    if(contentsModified.Container.ID == _container.ID)
                    {
                        if (ContainedEntities.Count == 0)
                        {
                            _automaticEngageToken?.Cancel();
                            _automaticEngageToken = null;
                        }
                        else
                        {
                            TryQueueEngage();
                        }
                    }
                    break;

                case EngageInserterMessage:
                    TryQueueEngage();
                    break;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if the inserter was able to increase its pressure</returns>
        private bool Pressurize(float frameTime)
        {
            if (!Owner.Transform.Coordinates.TryGetTileAtmosphere(out var tileAtmos) || tileAtmos.Air == null || tileAtmos.Air.Temperature > 0)
            {
                return false;
            }

            var tileAir = tileAtmos.Air;
            var transferMoles = 0.1f * (0.05f * Atmospherics.OneAtmosphere * 1.01f - Air.Pressure) * Air.Volume / (tileAir.Temperature * Atmospherics.R) * frameTime;

            Air = tileAir.Remove(transferMoles);

            var atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            atmosSystem.GetGridAtmosphere(Owner.Transform.GridID)?.Invalidate(tileAtmos.GridIndices);

            return true;
        }

        private void TryQueueEngage()
        {
            if (!Powered && ContainedEntities.Count == 0)
            {
                return;
            }

            _automaticEngageToken = new CancellationTokenSource();

            Owner.SpawnTimer(_automaticEngageTime, () =>
            {
                if (!TryFlush())
                {
                    TryQueueEngage();
                }
            }, _automaticEngageToken.Token);
        }

        private bool CanFlush()
        {
            return Air.Pressure >= _engagePressure && Powered && Anchored;
        }

        private void ToggleEngage()
        {
            Engaged ^= true;

            if (Engaged && CanFlush())
            {
                Owner.SpawnTimer(_flushDelay, () => TryFlush());
            }
        }

        private void PowerStateChanged(PowerChangedMessage args)
        {
            if (!args.Powered)
            {
                _automaticEngageToken?.Cancel();
                _automaticEngageToken = null;
            }

            if (Engaged && !TryFlush())
            {
                TryQueueEngage();
            }
        }

        [Verb]
        private sealed class FlushVerb : Verb<DisposalInserterComponent>
        {
            protected override void GetData(IEntity user, DisposalInserterComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user) ||
                    component.ContainedEntities.Contains(user))
                {
                    return;
                }

                data.Visibility = VerbVisibility.Visible;
                data.Text = Loc.GetString("Flush");
            }

            protected override void Activate(IEntity user, DisposalInserterComponent component)
            {
                component.Engaged = true;
                component.TryFlush();
            }
        }

        public class PressureChangedMessage : ComponentMessage
        {

            public float Pressure { get; }

            public PressureChangedMessage(float pressure)
            {
                Pressure = pressure;
            }
        }

        public class EngageInserterMessage : ComponentMessage {}

        public class InserterFlushedMessage : ComponentMessage {}
    }
}
