using System;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Shared.GameObjects.Components.Power;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Power.ApcNetComponents
{
    /// <summary>
    ///     Attempts to link with a nearby <see cref="IPowerProvider"/>s so that it can receive power from a <see cref="IApcNet"/>.
    /// </summary>
    [RegisterComponent]
    public class PowerReceiverComponent : Component, IExamine
    {
        [Dependency] private readonly IServerEntityManager _serverEntityManager = default!;

        public override string Name => "PowerReceiver";

        public event EventHandler<PowerStateEventArgs> OnPowerStateChanged;

        [ViewVariables]
        public bool Powered => (HasApcPower || !NeedsPower) && !PowerDisabled;

        [ViewVariables]
        public bool HasApcPower { get => _hasApcPower; set => SetHasApcPower(value); }
        private bool _hasApcPower;

        /// <summary>
        ///     The max distance from a <see cref="PowerProviderComponent"/> that this can receive power from.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int PowerReceptionRange { get => _powerReceptionRange; set => SetPowerReceptionRange(value); }
        private int _powerReceptionRange;

        [ViewVariables]
        public IPowerProvider Provider { get => _provider; set => SetProvider(value); }
        private IPowerProvider _provider = PowerProviderComponent.NullProvider;

        /// <summary>
        ///     If this should be considered for connection by <see cref="PowerProviderComponent"/>s.
        /// </summary>
        public bool Connectable => Anchored;

        private bool Anchored => !Owner.TryGetComponent<ICollidableComponent>(out var collidable) || collidable.Anchored;

        [ViewVariables]
        public bool NeedsProvider { get; private set; } = true;

        /// <summary>
        ///     Amount of charge this needs from an APC per second to function.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Load { get => _load; set => SetLoad(value); }
        private int _load;

        /// <summary>
        ///     When false, causes this to appear powered even if not receiving power from an Apc.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool NeedsPower { get => _needsPower; set => SetNeedsPower(value); }
        private bool _needsPower;

        /// <summary>
        ///     When true, causes this to never appear powered.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public bool PowerDisabled { get => _powerDisabled; set => SetPowerDisabled(value); }
        private bool _powerDisabled;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(ref _powerReceptionRange, "powerReceptionRange", 3);
            serializer.DataField(ref _load, "powerLoad", 5);
            serializer.DataField(ref _needsPower, "needsPower", true);
            serializer.DataField(ref _powerDisabled, "powerDisabled", false);
        }

        protected override void Startup()
        {
            base.Startup();
            if (NeedsProvider)
            {
                TryFindAndSetProvider();
            }
            if (Owner.TryGetComponent<ICollidableComponent>(out var collidable))
            {
                AnchorUpdate();
                collidable.AnchoredChanged += AnchorUpdate;
            }
        }

        public override void OnRemove()
        {
            if (Owner.TryGetComponent<ICollidableComponent>(out var collidable))
            {
                collidable.AnchoredChanged -= AnchorUpdate;
            }
            _provider.RemoveReceiver(this);
            base.OnRemove();
        }

        public void TryFindAndSetProvider()
        {
            if (TryFindAvailableProvider(out var provider))
            {
                Provider = provider;
            }
        }

        private bool TryFindAvailableProvider(out IPowerProvider foundProvider)
        {
            var nearbyEntities = _serverEntityManager
                .GetEntitiesInRange(Owner, PowerReceptionRange);

            foreach (var entity in nearbyEntities)
            {
                if (entity.TryGetComponent<PowerProviderComponent>(out var provider))
                {
                    if (provider.Connectable)
                    {
                        if (provider.Owner.Transform.Coordinates.TryDistance(_serverEntityManager, Owner.Transform.Coordinates, out var distance))
                        {
                            if (distance < Math.Min(PowerReceptionRange, provider.PowerTransferRange))
                            {
                                foundProvider = provider;
                                return true;
                            }
                        }
                    }
                }
            }
            foundProvider = default;
            return false;
        }

        public void ClearProvider()
        {
            _provider.RemoveReceiver(this);
            _provider = PowerProviderComponent.NullProvider;
            NeedsProvider = true;
            HasApcPower = false;
        }

        private void SetProvider(IPowerProvider newProvider)
        {
            _provider.RemoveReceiver(this);
            _provider = newProvider;
            newProvider.AddReceiver(this);
            NeedsProvider = false;
        }

        private void SetHasApcPower(bool newHasApcPower)
        {
            var oldPowered = Powered;
            _hasApcPower = newHasApcPower;
            if (oldPowered != Powered)
            {
                OnNewPowerState();
            }
        }

        private void SetPowerReceptionRange(int newPowerReceptionRange)
        {
            ClearProvider();
            _powerReceptionRange = newPowerReceptionRange;
            TryFindAndSetProvider();
        }

        private void SetLoad(int newLoad)
        {
            _load = newLoad;
        }

        private void SetNeedsPower(bool newNeedsPower)
        {
            var oldPowered = Powered;
            _needsPower = newNeedsPower;
            if (oldPowered != Powered)
            {
                OnNewPowerState();
            }
        }

        private void SetPowerDisabled(bool newPowerDisabled)
        {
            var oldPowered = Powered;
            _powerDisabled = newPowerDisabled;
            if (oldPowered != Powered)
            {
                OnNewPowerState();
            }
        }

        private void OnNewPowerState()
        {
            OnPowerStateChanged?.Invoke(this, new PowerStateEventArgs(Powered));
            if (Owner.TryGetComponent(out AppearanceComponent appearance))
            {
                appearance.SetData(PowerDeviceVisuals.Powered, Powered);
            }
        }

        private void AnchorUpdate()
        {
            if (Anchored)
            {
                if (NeedsProvider)
                {
                    TryFindAndSetProvider();
                }
            }
            else
            {
                ClearProvider();
            }
        }
        ///<summary>
        ///Adds some markup to the examine text of whatever object is using this component to tell you if it's powered or not, even if it doesn't have an icon state to do this for you.
        ///</summary>

        public void Examine(FormattedMessage message, bool inDetailsRange)
        {
            message.AddMarkup(Loc.GetString("It appears to be {0}.", this.Powered ? "[color=darkgreen]powered[/color]" : "[color=darkred]un-powered[/color]"));
        }
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
