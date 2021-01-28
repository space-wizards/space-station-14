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
    /// <summary>
    /// Called before trying to flush. Use for extra can flush checks.
    /// </summary>
    /// <returns>Inserter won't flush if false</returns>
    //public delegate bool OnPreFlushCheck();

    /// <summary>
    /// Is called before the default way of inserting entities into the disposal entry.
    /// Use when you want to handle inserting differently.
    /// </summary>
    /// <param name="entry"></param>
    /// <returns>Whether inserting was handeled</returns>
    //public delegate bool OnHandleInsert(DisposalEntryComponent entry);

    //TODO: Write documentation
    /// <summary>
    /// 
    /// </summary>
    [RegisterComponent]
    public class DisposalInserterComponent : Component, IGasMixtureHolder
    {
        [ComponentDependency]
        public readonly PowerReceiverComponent? PowerReceiver = null;

        [ComponentDependency]
        public readonly IPhysicsComponent? Physics = null;

        [ComponentDependency]
        public readonly SnapGridComponent? SnapGrid = null;

        public override string Name => "DisposalInserter";

        [ViewVariables]
        public bool Anchored => Physics != null && Physics.Anchored;

        /// <summary>
        /// Used by the disposal entry component to get the entities from the inserter
        /// </summary>
        [ViewVariables]
        public IReadOnlyList<IEntity> ContainedEntities => _container != null ? _container.ContainedEntities : new List<IEntity>();

        [ViewVariables]
        public bool Powered => PowerReceiver != null && PowerReceiver.Powered;

        [ViewVariables]
        public GasMixture Air { get; set; } = default!;

        public bool Engaged { get; set; }

        /// <summary>
        /// When the inserter is in manual mode it sends a <see cref="ManualFlushReadyMessage"/> event and inserts when receiving a <see cref="ManualInsertMessage"/>
        /// </summary>
        [ViewVariables]
        public bool ManualMode = false;

        /// <summary>
        ///     The engage pressure of this inserter.
        ///     Prevents it from flushing if the air pressure inside it is not equal to or bigger than this.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        private float _engagePressure;

        [ViewVariables(VVAccess.ReadWrite)]
        private int _pumpRate;

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

        private bool _pressurized = false;
        private bool _manualFlushReady = false;
        private bool _queuedFlush = false;
        private bool _flush = false;

        /// <summary>
        /// 
        /// </summary>
        public void Update(float frameTime)
        {
            if (!Powered || Air.Pressure < _engagePressure && !Pressurize(frameTime))
            {
                return;
            }

            if(_flush || _queuedFlush)
            {
                var flushFailed = !TryFlush();
                if(flushFailed)
                {
                    SendMessage(new InserterFlushedMessage(true));
                    if(_queuedFlush)
                    {
                        TryQueueEngage();
                    }
                }
                _flush = false;
                _queuedFlush = false;
            }

            if(Air.Pressure >= _engagePressure)
            {
                if (Powered && !_pressurized)
                {
                    SendMessage(new PressureChangedMessage(Air.Pressure, _engagePressure));
                    _pressurized = true;
                }

                if (Engaged && CanFlush())
                {
                    Owner.SpawnTimer(_flushDelay, () => { _flush = true; });
                    Engaged = false;
                }
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
                "pumpRate",
                1,
                pumpRate => _pumpRate = pumpRate,
                () => _pumpRate);

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
            serializer.DataField(ref ManualMode, "manualInsertMode", false);
        }

        public bool TryFlush()
        {
            if (!CanFlush() || !TryGetEntry(out var entry))
            {
                return false;
            }

            _automaticEngageToken?.Cancel();
            _automaticEngageToken = null;

            if (ManualMode)
            {
                _manualFlushReady = true;
                SendMessage(new ManualFlushReadyMessage());
                return true;
            }

            var entryComponent = entry.GetComponent<DisposalEntryComponent>();

            entryComponent.TryInsert(this);

            _manualFlushReady = false;
            SendMessage(new InserterFlushedMessage());

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

                case EngageInserterMessage engageMessage:
                    Engaged = engageMessage.Engage;
                    break;

                case ManualInsertMessage insertMessage:
                    if(ManualMode)
                    {
                        ManualInsert(insertMessage.Holder);
                    }
                    break;
            }
        }

        protected override void Startup()
        {
            base.Startup();

            if (_container == default!)
            {
                _container = ContainerManagerComponent.Ensure<Container>(_containerID, Owner);
            }

            SendMessage(new PressureChangedMessage(Air.Pressure, _engagePressure));
        }

        private void ManualInsert(IEntity holder)
        {
            if (!_manualFlushReady || !CanFlush() || !TryGetEntry(out var entry) || !holder.TryGetComponent<DisposalHolderComponent>(out var holderComponent))
            {
                SendMessage(new InserterFlushedMessage(true));
                return;
            }

            var entryComponent = entry.GetComponent<DisposalEntryComponent>();

            entryComponent.TryInsert(holderComponent, Air);

            _manualFlushReady = false;
            SendMessage(new InserterFlushedMessage());
        }

        private bool TryGetEntry(out IEntity entry)
        {
            if (SnapGrid == null)
            {
                entry = default!;
                return false;
            }

            var entity = SnapGrid.GetLocal().FirstOrDefault(entity => entity.HasComponent<DisposalEntryComponent>());

            if (entity == null)
            {
                entry = default!;
                return false;
            }

            entry = entity;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>true if the inserter was able to increase its pressure</returns>
        private bool Pressurize(float frameTime)
        {
            if (!Owner.Transform.Coordinates.TryGetTileAtmosphere(out var tileAtmos) || tileAtmos.Air == null || tileAtmos.Air.Temperature <= 0)
            {
                return false;
            }

            _pressurized = false;

            var tileAir = tileAtmos.Air;
            //var transferMoles = 0.1f * (0.05f * Atmospherics.OneAtmosphere - Air.Pressure) * Air.Volume / (tileAir.Temperature * Atmospherics.R);
            var volumeRatio = Math.Clamp(_pumpRate / tileAir.Volume, 0, 1);
            
            Air.Merge(tileAir.RemoveRatio(volumeRatio));

            var atmosSystem = EntitySystem.Get<AtmosphereSystem>();
            atmosSystem.GetGridAtmosphere(Owner.Transform.GridID)?.Invalidate(tileAtmos.GridIndices);

            SendMessage(new PressureChangedMessage(Air.Pressure, _engagePressure));

            return true;
        }

        private void TryQueueEngage()
        {
            if (!Powered && ContainedEntities.Count == 0 || _automaticEngageTime == TimeSpan.Zero)
            {
                return;
            }

            _automaticEngageToken = new CancellationTokenSource();

            Owner.SpawnTimer(_automaticEngageTime, () => { _queuedFlush = true; }, _automaticEngageToken.Token);
        }

        private bool CanFlush()
        {
            return Air.Pressure >= _engagePressure && Powered && Anchored;
        }

        private void PowerStateChanged(PowerChangedMessage args)
        {
            if (!args.Powered || !Engaged)
            {
                _automaticEngageToken?.Cancel();
                _automaticEngageToken = null;
                return;
            }

            _queuedFlush = true;
        }

        [Verb]
        private sealed class FlushVerb : Verb<DisposalInserterComponent>
        {
            protected override void GetData(IEntity user, DisposalInserterComponent component, VerbData data)
            {
                data.Visibility = VerbVisibility.Invisible;

                if (!ActionBlockerSystem.CanInteract(user) || component.ContainedEntities.Contains(user) || component.ManualMode)
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

        /// <summary>
        /// Gets sent when the pressure inside the inserter changes
        /// </summary>
        public class PressureChangedMessage : ComponentMessage
        {

            public float Pressure { get; }
            public float TargetPressure { get; }

            public PressureChangedMessage(float pressure, float targetPressure)
            {
                Pressure = pressure;
                TargetPressure = targetPressure;
            }
        }

        /// <summary>
        /// The inserter engages or disengages when it receives this message
        /// </summary>
        public class EngageInserterMessage : ComponentMessage
        {
            public bool Engage { get; }

            public EngageInserterMessage(bool engage)
            {
                Engage = engage;
            }
        }

        /// <summary>
        /// Is sent when the inserter has flushed
        /// </summary>
        public class InserterFlushedMessage : ComponentMessage {
            public bool Failed { get; }

            public InserterFlushedMessage(bool failed = false)
            {
                Failed = failed;
            }
        }

        /// <summary>
        /// Is sent when the inserter is in manual mode, has been engaged and is ready to flush
        /// </summary>
        public class ManualFlushReadyMessage : ComponentMessage {}

        /// <summary>
        /// The inserter inserts the given disposal holder when it`s in manual mode and received this message
        /// </summary>
        public class ManualInsertMessage : ComponentMessage
        {
            public IEntity Holder { get; }

            public ManualInsertMessage(IEntity holder)
            {
                Holder = holder;
            }
        }

    }
}
