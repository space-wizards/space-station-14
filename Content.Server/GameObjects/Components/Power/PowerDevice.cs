using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power
{
    /// <summary>
    /// Component that requires power to function
    /// </summary>
    [RegisterComponent]
    public class PowerDeviceComponent : Component, EntitySystems.IExamine
    {
        public override string Name => "PowerDevice";

        protected override void Startup()
        {
            base.Startup();
            if (_drawType != DrawTypes.Node)
            {
                var componentMgr = IoCManager.Resolve<IComponentManager>();
                AvailableProviders = componentMgr.GetAllComponents<PowerProviderComponent>().Where(x => x.CanServiceDevice(this)).ToList();
                ConnectToBestProvider();
            }
        }

        /// <summary>
        ///     The method of draw we will try to use to place our load set via component parameter, defaults to using power providers
        /// </summary>
        [ViewVariables]
        public virtual DrawTypes DrawType
        {
            get => _drawType;
            protected set => _drawType = value;
        }
        private DrawTypes _drawType = DrawTypes.Provider;

        /// <summary>
        ///     The power draw method we are currently connected to and using
        /// </summary>
        [ViewVariables]
        public DrawTypes Connected { get; protected set; } = DrawTypes.None;

        [ViewVariables]
        public bool Powered { get; private set; } = false;


        /// <summary>
        ///     Is an external power source currently available?
        /// </summary>
        [ViewVariables]
        public bool ExternalPowered
        {
            get => _externalPowered;
            set
            {
                _externalPowered = value;
                UpdatePowered();
            }
        }
        private bool _externalPowered = false;

        /// <summary>
        ///     Is an internal power source currently available?
        /// </summary>
        [ViewVariables]
        public bool InternalPowered
        {
            get => _internalPowered;
            set
            {
                _internalPowered = value;
                UpdatePowered();
            }
        }
        private bool _internalPowered = false;

        /// <summary>
        /// Priority for powernet draw, lower will draw first, defined in powernet.cs
        /// </summary>
        [ViewVariables]
        public virtual Powernet.Priority Priority
        {
            get => _priority;
            protected set => _priority = value;
        }
        private Powernet.Priority _priority = Powernet.Priority.Medium;

        private float _load = 100; //arbitrary magic number to start
        /// <summary>
        ///     Power load from this entity.
        ///     In Watts.
        /// </summary>
        [ViewVariables]
        public float Load
        {
            get => _load;
            set => UpdateLoad(value);
        }

        /// <summary>
        /// All the power providers that we are within range of
        /// </summary>
        public List<PowerProviderComponent> AvailableProviders = new List<PowerProviderComponent>();


        private PowerProviderComponent _provider;

        /// <summary>
        /// A power provider that will handle our load, if we are linked to any
        /// </summary>
        [ViewVariables]
        public PowerProviderComponent Provider
        {
            get => _provider;
            set
            {
                Connected = DrawTypes.Provider;
                if (_provider != null)
                {
                    _provider.RemoveDevice(this);
                }

                _provider = value;
                if (value != null)
                {
                    _provider.AddDevice(this);
                }
                else
                {
                    Connected = DrawTypes.None;
                }

            }
        }

        public event EventHandler<PowerStateEventArgs> OnPowerStateChanged;

        public override void OnAdd()
        {
            base.OnAdd();

            if (DrawType == DrawTypes.Node || DrawType == DrawTypes.Both)
            {
                if (!Owner.TryGetComponent(out PowerNodeComponent node))
                {
                    Owner.AddComponent<PowerNodeComponent>();
                    node = Owner.GetComponent<PowerNodeComponent>();
                }
                node.OnPowernetConnect += PowernetConnect;
                node.OnPowernetDisconnect += PowernetDisconnect;
                node.OnPowernetRegenerate += PowernetRegenerate;
            }
        }

        /// <inheritdoc />
        protected override void Shutdown()
        {
            if (Owner.TryGetComponent(out PowerNodeComponent node))
            {
                if (node.Parent != null && node.Parent.HasDevice(this))
                {
                    node.Parent.RemoveDevice(this);
                }

                node.OnPowernetConnect -= PowernetConnect;
                node.OnPowernetDisconnect -= PowernetDisconnect;
                node.OnPowernetRegenerate -= PowernetRegenerate;
            }

            Connected = DrawTypes.None;

            if (Provider != null)
            {
                Provider = null;
            }

            base.Shutdown();
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _drawType, "drawtype", DrawTypes.Provider);
            serializer.DataField(ref _load, "load", 100);
            serializer.DataField(ref _priority, "priority", Powernet.Priority.Medium);
        }

        void IExamine.Examine(FormattedMessage message)
        {
            if (!Powered)
            {
                message.AddText("The device is not powered.");
            }
        }

        private void UpdateLoad(float value)
        {
            var oldLoad = _load;
            _load = value;
            if (Connected == DrawTypes.Node)
            {
                var node = Owner.GetComponent<PowerNodeComponent>();
                node.Parent.UpdateDevice(this, oldLoad);
            }
            else if (Connected == DrawTypes.Provider)
            {
                Provider.UpdateDevice(this, oldLoad);
            }
        }

        /// <summary>
        ///     Updates the state of whether or not this device is powered,
        ///     and fires off events if said state has changed.
        /// </summary>
        private void UpdatePowered()
        {
            var oldPowered = Powered;
            Powered = ExternalPowered || InternalPowered;
            if (oldPowered != Powered)
            {
                if (Powered)
                {
                    OnPowerStateChanged?.Invoke(this, new PowerStateEventArgs(true));
                }
                else
                {
                    OnPowerStateChanged?.Invoke(this, new PowerStateEventArgs(false));
                }
            }
        }

        /// <summary>
        /// Register a new power provider as a possible connection to this device
        /// </summary>
        /// <param name="provider"></param>
        public void AddProvider(PowerProviderComponent provider)
        {
            AvailableProviders.Add(provider);

            if (Connected != DrawTypes.Node)
            {
                ConnectToBestProvider();
            }
        }

        /// <summary>
        /// Find the nearest registered power provider and connect to it
        /// </summary>
        private void ConnectToBestProvider()
        {
            //Any values we can connect to or are we already connected to a node, cancel!
            if (!AvailableProviders.Any() || Connected == DrawTypes.Node || Deleted)
                return;

            //Get the starting value for our loop
            var position = Owner.GetComponent<ITransformComponent>().WorldPosition;
            var bestprovider = AvailableProviders[0];

            //If we are already connected to a power provider we need to do a loop to find the nearest one, otherwise skip it and use first entry
            if (Connected == DrawTypes.Provider)
            {
                var bestdistance = (bestprovider.Owner.GetComponent<ITransformComponent>().WorldPosition - position).LengthSquared;

                foreach (var availprovider in AvailableProviders)
                {
                    //Find distance to new provider
                    var distance = (availprovider.Owner.GetComponent<ITransformComponent>().WorldPosition - position).LengthSquared;

                    //If new provider distance is shorter it becomes new best possible provider
                    if (distance < bestdistance)
                    {
                        bestdistance = distance;
                        bestprovider = availprovider;
                    }
                }
            }

            if (Provider != bestprovider)
                Provider = bestprovider;
        }

        /// <summary>
        /// Remove a power provider from being a possible connection to this device
        /// </summary>
        /// <param name="provider"></param>
        public void RemoveProvider(PowerProviderComponent provider)
        {
            if (!AvailableProviders.Contains(provider))
                return;

            AvailableProviders.Remove(provider);

            if (provider == Provider)
            {
                Provider = null;
                ExternalPowered = false;
            }

            if (Connected != DrawTypes.Node)
            {
                ConnectToBestProvider();
            }
        }

        /// <summary>
        /// Node has become anchored to a powernet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        protected virtual void PowernetConnect(object sender, PowernetEventArgs eventarg)
        {
            //This sets connected = none so it must be first
            Provider = null;

            eventarg.Powernet.AddDevice(this);
            Connected = DrawTypes.Node;
        }

        /// <summary>
        /// Powernet wire was remove so we need to regenerate the powernet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        protected virtual void PowernetRegenerate(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.AddDevice(this);
        }

        /// <summary>
        /// Node has become unanchored from a powernet
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventarg"></param>
        protected virtual void PowernetDisconnect(object sender, PowernetEventArgs eventarg)
        {
            eventarg.Powernet.RemoveDevice(this);
            Connected = DrawTypes.None;

            ConnectToBestProvider();
        }

        /// <summary>
        ///     Process mechanism to keep track of internal battery and power status.
        /// </summary>
        /// <param name="frametime">Time since the last process frame.</param>
        internal virtual void ProcessInternalPower(float frametime)
        {
            if (Owner.TryGetComponent<PowerStorageComponent>(out var storage) && storage.CanDeductCharge(Load))
            {
                // We still keep InternalPowered correct if connected externally,
                // but don't use it.
                if (!ExternalPowered)
                {
                    storage.DeductCharge(Load);
                }
                InternalPowered = true;
            }
            else
            {
                InternalPowered = false;
            }
        }
    }

    /// <summary>
    /// The different methods that a <see cref="PowerDeviceComponent"/> can use to connect to a power network.
    /// </summary>
    public enum DrawTypes
    {
        /// <summary>
        /// This device cannot be connected to a power network.
        /// </summary>
        None = 0,

        /// <summary>
        /// This device can connect to a <see cref=""/>
        /// </summary>
        Node = 1,
        Provider = 2,
        Both = 3,
    }

    public class PowerStateEventArgs : EventArgs
    {
        public readonly bool Powered;

        public PowerStateEventArgs(bool powered)
        {
            Powered = powered;
        }
    }
}
