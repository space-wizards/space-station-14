using System;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Content.Server.GameObjects.Components.VendingMachines;
using Content.Server.GameObjects.EntitySystems;
using Content.Server.Interfaces;
using Content.Server.Interfaces.GameObjects;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Server.GameObjects.Components.UserInterface;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using Robust.Shared.Utility;

namespace Content.Server.GameObjects.Components
{
    [RegisterComponent]
    public class WiresComponent : SharedWiresComponent, IInteractUsing, IExamine
    {
#pragma warning disable 649
        [Dependency] private readonly IRobustRandom _random;
        [Dependency] private readonly IServerNotifyManager _notifyManager;
        [Dependency] private readonly ILocalizationManager _localizationManager;
#pragma warning restore 649
        private AudioSystem _audioSystem;
        private AppearanceComponent _appearance;
        private BoundUserInterface _userInterface;

        private bool _isPanelOpen;
        /// <summary>
        /// Opening the maintenance panel (typically with a screwdriver) changes this.
        /// </summary>
        public bool IsPanelOpen
        {
            get => _isPanelOpen;
            private set
            {
                if (_isPanelOpen == value)
                {
                    return;
                }
                _isPanelOpen = value;
                UpdateAppearance();
            }
        }

        private bool _isPanelVisible = true;
        /// <summary>
        /// Components can set this to prevent the maintenance panel overlay from showing even if it's open
        /// </summary>
        public bool IsPanelVisible
        {
            get => _isPanelVisible;
            set
            {
                if (_isPanelVisible == value)
                {
                    return;
                }
                _isPanelVisible = value;
                UpdateAppearance();
            }
        }

        private void UpdateAppearance()
        {
            _appearance.SetData(WiresVisuals.MaintenancePanelState, IsPanelOpen && IsPanelVisible);
        }

        /// <summary>
        /// Contains all registered wires.
        /// </summary>
        public readonly List<Wire> WiresList = new List<Wire>();

        /// <summary>
        /// Status messages are displayed at the bottom of the UI.
        /// </summary>
        private readonly Dictionary<object, string> _statuses = new Dictionary<object, string>();

        /// <summary>
        /// <see cref="AssignColor"/> and <see cref="WiresBuilder.CreateWire"/>.
        /// </summary>
        private readonly List<Color> _availableColors = new List<Color>()
        {
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.Orange,
            Color.Brown,
            Color.Gold,
            Color.Gray,
            Color.Cyan,
            Color.Navy,
            Color.Purple,
            Color.Pink,
            Color.Fuchsia,
        };

        public override void Initialize()
        {
            base.Initialize();
            _audioSystem = EntitySystem.Get<AudioSystem>();
            _appearance = Owner.GetComponent<AppearanceComponent>();
            _appearance.SetData(WiresVisuals.MaintenancePanelState, IsPanelOpen);
            _userInterface = Owner.GetComponent<ServerUserInterfaceComponent>()
                .GetBoundUserInterface(WiresUiKey.Key);
            _userInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
        }

        protected override void Startup()
        {
            base.Startup();

            foreach (var wiresProvider in Owner.GetAllComponents<IWires>())
            {
                var builder = new WiresBuilder(this, wiresProvider);
                wiresProvider.RegisterWires(builder);
            }

            UpdateUserInterface();
        }

        /// <summary>
        /// Returns whether the wire associated with <see cref="identifier"/> is cut.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public bool IsWireCut(object identifier)
        {
            var wire = WiresList.Find(x => x.Identifier.Equals(identifier));
            if(wire == null) throw new ArgumentException();
            return wire.IsCut;
        }

        public class Wire
        {
            /// <summary>
            /// Used in client-server communication to identify a wire without telling the client what the wire does.
            /// </summary>
            public readonly Guid Guid;
            /// <summary>
            /// Registered by components implementing IWires, used to identify which wire the client interacted with.
            /// </summary>
            public readonly object Identifier;
            /// <summary>
            /// The color of the wire. It needs to have a corresponding entry in <see cref="Robust.Shared.Maths.Color.DefaultColors"/>.
            /// </summary>
            public readonly Color Color;
            /// <summary>
            /// The component that registered the wire.
            /// </summary>
            public readonly IWires Owner;
            /// <summary>
            /// Whether the wire is cut.
            /// </summary>
            public bool IsCut;
            public Wire(Guid guid, object identifier, Color color, IWires owner, bool isCut)
            {
                Guid = guid;
                Identifier = identifier;
                Color = color;
                Owner = owner;
                IsCut = isCut;
            }
        }

        /// <summary>
        /// Used by <see cref="IWires.RegisterWires"/>.
        /// </summary>
        public class WiresBuilder
        {
            [NotNull] private readonly WiresComponent _wires;
            [NotNull] private readonly IWires _owner;

            public WiresBuilder(WiresComponent wires, IWires owner)
            {
                _wires = wires;
                _owner = owner;
            }

            public void CreateWire(object identifier, Color? color = null, bool isCut = false)
            {
                if (!color.HasValue)
                {
                    color = _wires.AssignColor();
                }
                else
                {
                    _wires._availableColors.Remove(color.Value);
                }
                _wires.WiresList.Add(new Wire(Guid.NewGuid(), identifier, color.Value, _owner, isCut));
            }
        }

        /// <summary>
        /// Picks a color from <see cref="_availableColors"/> and removes it from the list.
        /// </summary>
        /// <returns>The picked color.</returns>
        private Color AssignColor()
        {
            if(_availableColors.Count == 0)
            {
                return Color.Black;
            }
            return _random.PickAndTake(_availableColors);
        }

        /// <summary>
        /// Call this from other components to open the wires UI.
        /// </summary>
        public void OpenInterface(IPlayerSession session)
        {
            _userInterface.Open(session);
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            var message = serverMsg.Message;
            switch (message)
            {
                case WiresActionMessage msg:
                    var wire = WiresList.Find(x => x.Guid == msg.Guid);
                    var player = serverMsg.Session.AttachedEntity;
                    if (!player.TryGetComponent(out IHandsComponent handsComponent))
                    {
                        _notifyManager.PopupMessage(Owner.Transform.GridPosition, player, _localizationManager.GetString("You have no hands."));
                        return;
                    }

                    if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(player.Transform.MapPosition, Owner.Transform.WorldPosition, ignoredEnt: Owner))
                    {
                        _notifyManager.PopupMessage(Owner.Transform.GridPosition, player, _localizationManager.GetString("You can't reach there!"));
                        return;
                    }

                    var activeHandEntity = handsComponent.GetActiveHand?.Owner;
                    switch (msg.Action)
                    {
                        case WiresAction.Cut:
                            if (activeHandEntity?.HasComponent<WirecutterComponent>() != true)
                            {
                                _notifyManager.PopupMessage(Owner.Transform.GridPosition, player, _localizationManager.GetString("You need to hold a wirecutter in your hand!"));
                                return;
                            }
                            _audioSystem.Play("/Audio/items/wirecutter.ogg", Owner);
                            wire.IsCut = true;
                            UpdateUserInterface();
                            break;
                        case WiresAction.Mend:
                            if (activeHandEntity?.HasComponent<WirecutterComponent>() != true)
                            {
                                _notifyManager.PopupMessage(Owner.Transform.GridPosition, player, _localizationManager.GetString("You need to hold a wirecutter in your hand!"));
                                return;
                            }
                            _audioSystem.Play("/Audio/items/wirecutter.ogg", Owner);
                            wire.IsCut = false;
                            UpdateUserInterface();
                            break;
                        case WiresAction.Pulse:
                            if (activeHandEntity?.HasComponent<MultitoolComponent>() != true)
                            {
                                _notifyManager.PopupMessage(Owner.Transform.GridPosition, player, _localizationManager.GetString("You need to hold a multitool in your hand!"));
                                return;
                            }
                            if (wire.IsCut)
                            {
                                _notifyManager.PopupMessage(Owner.Transform.GridPosition, player, _localizationManager.GetString("You can't pulse a wire that's been cut!"));
                                return;
                            }
                            _audioSystem.Play("/Audio/effects/multitool_pulse.ogg", Owner);
                            break;
                    }
                    wire.Owner.WiresUpdate(new WiresUpdateEventArgs(wire.Identifier, msg.Action));
                    break;
            }
        }

        private void UpdateUserInterface()
        {
            var clientList = new List<ClientWire>();
            foreach (var entry in WiresList)
            {
                clientList.Add(new ClientWire(entry.Guid, entry.Color, entry.IsCut));
            }
            _userInterface.SetState(new WiresBoundUserInterfaceState(clientList, _statuses.Values.ToList()));
        }

        bool IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!eventArgs.Using.HasComponent<ScrewdriverComponent>())
            {
                return false;
            }

            IsPanelOpen = !IsPanelOpen;
            EntitySystem.Get<AudioSystem>()
                .Play(IsPanelOpen ? "/Audio/machines/screwdriveropen.ogg" : "/Audio/machines/screwdriverclose.ogg");
            return true;
        }

        void IExamine.Examine(FormattedMessage message)
        {
            var loc = IoCManager.Resolve<ILocalizationManager>();

            message.AddMarkup(loc.GetString(IsPanelOpen
                ? "The [color=lightgray]maintenance panel[/color] is [color=darkgreen]open[/color]."
                : "The [color=lightgray]maintenance panel[/color] is [color=darkred]closed[/color]."));
        }

        public void SetStatus(object statusIdentifier, string newMessage)
        {
            if (_statuses.TryGetValue(statusIdentifier, out var storedMessage))
            {
                if (storedMessage == newMessage)
                {
                    return;
                }
            }
            _statuses[statusIdentifier] = newMessage;
            UpdateUserInterface();
        }
    }
}
