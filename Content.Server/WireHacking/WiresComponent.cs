using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.DoAfter;
using Content.Server.Hands.Components;
using Content.Server.Tools;
using Content.Server.Tools.Components;
using Content.Server.UserInterface;
using Content.Server.VendingMachines;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Helpers;
using Content.Shared.Popups;
using Content.Shared.Sound;
using Content.Shared.Tools;
using Content.Shared.Wires;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.ViewVariables;

namespace Content.Server.WireHacking
{
    [RegisterComponent]
    public sealed class WiresComponent : SharedWiresComponent, IInteractUsing
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IEntityManager _entities = default!;

        private bool _isPanelOpen;

        [DataField("cuttingTime")] public float CuttingTime = 1f;

        [DataField("mendTime")] public float MendTime = 1f;

        [DataField("pulseTime")] public float PulseTime = 3f;

        [DataField("screwingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string ScrewingQuality = "Screwing";

        [DataField("cuttingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string CuttingQuality = "Cutting";

        [DataField("pulsingQuality", customTypeSerializer:typeof(PrototypeIdSerializer<ToolQualityPrototype>))]
        public string PulsingQuality = "Pulsing";

        /// <summary>
        /// Make do_afters for hacking unique per wire so we can't spam a single wire.
        /// </summary>
        public HashSet<int> PendingDoAfters = new();

        /// <summary>
        /// Opening the maintenance panel (typically with a screwdriver) changes this.
        /// </summary>
        [ViewVariables]
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

                if (!_isPanelOpen)
                    UserInterface?.CloseAll();
                UpdateAppearance();
            }
        }

        private bool _isPanelVisible = true;

        /// <summary>
        /// Components can set this to prevent the maintenance panel overlay from showing even if it's open
        /// </summary>
        [ViewVariables]
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

        [ViewVariables(VVAccess.ReadWrite)]
        public string BoardName
        {
            get => _boardName;
            set
            {
                _boardName = value;
                UpdateUserInterface();
            }
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public string? SerialNumber
        {
            get => _serialNumber;
            set
            {
                _serialNumber = value;
                UpdateUserInterface();
            }
        }

        private void UpdateAppearance()
        {
            if (_entities.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(WiresVisuals.MaintenancePanelState, IsPanelOpen && IsPanelVisible);
            }
        }

        /// <summary>
        /// Contains all registered wires.
        /// </summary>
        [ViewVariables]
        public readonly List<Wire> WiresList = new();

        /// <summary>
        /// Status messages are displayed at the bottom of the UI.
        /// </summary>
        [ViewVariables]
        private readonly Dictionary<object, object> _statuses = new();

        /// <summary>
        /// <see cref="AssignAppearance"/> and <see cref="WiresBuilder.CreateWire"/>.
        /// </summary>
        private readonly List<WireColor> _availableColors =
            new((WireColor[]) Enum.GetValues(typeof(WireColor)));

        private readonly List<WireLetter> _availableLetters =
            new((WireLetter[]) Enum.GetValues(typeof(WireLetter)));

        [DataField("BoardName")]
        private string _boardName = "Wires";

        [DataField("SerialNumber")]
        private string? _serialNumber;

        // Used to generate wire appearance randomization client side.
        // We honestly don't care what it is or such but do care that it doesn't change between UI re-opens.
        [ViewVariables]
        [DataField("WireSeed")]
        public int WireSeed;
        [ViewVariables]
        [DataField("LayoutId")]
        public string? LayoutId = default;

        [DataField("pulseSound")] public SoundSpecifier PulseSound = new SoundPathSpecifier("/Audio/Effects/multitool_pulse.ogg");

        [DataField("screwdriverOpenSound")]
        private SoundSpecifier _screwdriverOpenSound = new SoundPathSpecifier("/Audio/Machines/screwdriveropen.ogg");

        [DataField("screwdriverCloseSound")]
        private SoundSpecifier _screwdriverCloseSound = new SoundPathSpecifier("/Audio/Machines/screwdriverclose.ogg");

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(WiresUiKey.Key);

        protected override void Initialize()
        {
            base.Initialize();

            if (_entities.TryGetComponent(Owner, out AppearanceComponent? appearance))
            {
                appearance.SetData(WiresVisuals.MaintenancePanelState, IsPanelOpen);
            }

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += UserInterfaceOnReceiveMessage;
            }
        }

        /// <summary>
        /// Returns whether the wire associated with <see cref="identifier"/> is cut.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public bool IsWireCut(object identifier)
        {
            var wire = WiresList.Find(x => x.Identifier.Equals(identifier));
            if (wire == null) throw new ArgumentException();
            return wire.IsCut;
        }

        public sealed class Wire
        {
            /// <summary>
            /// The component that registered the wire.
            /// </summary>
            public IWires Owner { get; }

            /// <summary>
            /// Whether the wire is cut.
            /// </summary>
            public bool IsCut { get; set; }

            /// <summary>
            /// Used in client-server communication to identify a wire without telling the client what the wire does.
            /// </summary>
            [ViewVariables]
            public int Id { get; set; }

            /// <summary>
            /// The color of the wire.
            /// </summary>
            [ViewVariables]
            public WireColor Color { get; }

            /// <summary>
            /// The greek letter shown below the wire.
            /// </summary>
            [ViewVariables]
            public WireLetter Letter { get; }

            /// <summary>
            /// Registered by components implementing IWires, used to identify which wire the client interacted with.
            /// </summary>
            [ViewVariables]
            public object Identifier { get; }

            public Wire(IWires owner, bool isCut, WireColor color, WireLetter letter, object identifier)
            {
                Owner = owner;
                IsCut = isCut;
                Color = color;
                Letter = letter;
                Identifier = identifier;
            }
        }

        /// <summary>
        /// Used by <see cref="IWires.RegisterWires"/>.
        /// </summary>
        public sealed class WiresBuilder
        {
            private readonly WiresComponent _wires;
            private readonly IWires _owner;
            private readonly WireLayout? _layout;

            public WiresBuilder(WiresComponent wires, IWires owner, WireLayout? layout)
            {
                _wires = wires;
                _owner = owner;
                _layout = layout;
            }

            public void CreateWire(object identifier, (WireColor, WireLetter)? appearance = null, bool isCut = false)
            {
                WireLetter letter;
                WireColor color;
                if (!appearance.HasValue)
                {
                    if (_layout != null && _layout.Specifications.TryGetValue(identifier, out var specification))
                    {
                        color = specification.Color;
                        letter = specification.Letter;
                        _wires._availableColors.Remove(color);
                        _wires._availableLetters.Remove(letter);
                    }
                    else
                    {
                        (color, letter) = _wires.AssignAppearance();
                    }
                }
                else
                {
                    (color, letter) = appearance.Value;
                    _wires._availableColors.Remove(color);
                    _wires._availableLetters.Remove(letter);
                }

                // TODO: ENSURE NO RANDOM OVERLAP.
                _wires.WiresList.Add(new Wire(_owner, isCut, color, letter, identifier));
            }
        }

        /// <summary>
        /// Picks a color from <see cref="_availableColors"/> and removes it from the list.
        /// </summary>
        /// <returns>The picked color.</returns>
        private (WireColor, WireLetter) AssignAppearance()
        {
            var color = _availableColors.Count == 0 ? WireColor.Red : _random.PickAndTake(_availableColors);
            var letter = _availableLetters.Count == 0 ? WireLetter.α : _random.PickAndTake(_availableLetters);

            return (color, letter);
        }

        /// <summary>
        /// Call this from other components to open the wires UI.
        /// </summary>
        public void OpenInterface(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        /// <summary>
        /// Closes all wire UIs.
        /// </summary>
        public void CloseAll()
        {
            UserInterface?.CloseAll();
        }

        public bool CanWiresInteract(EntityUid user, [NotNullWhen(true)] out ToolComponent? tool)
        {
            tool = null;

            if (!_entities.TryGetComponent(user, out HandsComponent? handsComponent))
            {
                Owner.PopupMessage(user, Loc.GetString("wires-component-ui-on-receive-message-no-hands"));
                return false;
            }

            if (!EntitySystem.Get<SharedInteractionSystem>().InRangeUnobstructed(user, Owner))
            {
                Owner.PopupMessage(user, Loc.GetString("wires-component-ui-on-receive-message-cannot-reach"));
                return false;
            }

            if (handsComponent.GetActiveHand()?.HeldEntity is not { Valid: true } activeHandEntity ||
                !_entities.TryGetComponent(activeHandEntity, out tool))
            {
                return false;
            }

            return true;
        }

        private void UserInterfaceOnReceiveMessage(ServerBoundUserInterfaceMessage serverMsg)
        {
            var message = serverMsg.Message;
            switch (message)
            {
                case WiresActionMessage msg:
                    var wire = WiresList.Find(x => x.Id == msg.Id);
                    if (wire == null ||
                        serverMsg.Session.AttachedEntity is not {} player ||
                        PendingDoAfters.Contains(wire.Id))
                    {
                        return;
                    }

                    if (!CanWiresInteract(player, out var tool))
                        return;

                    var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

                    switch (msg.Action)
                    {
                        case WiresAction.Cut:
                            if (!tool.Qualities.Contains(CuttingQuality))
                            {
                                player.PopupMessageCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"));
                                return;
                            }

                            doAfterSystem.DoAfter(
                                new DoAfterEventArgs(player, CuttingTime, target: Owner)
                                {
                                    TargetFinishedEvent = new WiresCutEvent
                                    {
                                        Wire = wire,
                                        Tool = tool,
                                        User = player,
                                    },
                                    TargetCancelledEvent = new WiresCancelledEvent()
                                    {
                                        Wire = wire,
                                    },
                                    NeedHand = true,
                                });

                            PendingDoAfters.Add(wire.Id);

                            break;
                        case WiresAction.Mend:
                            if (!tool.Qualities.Contains(CuttingQuality))
                            {
                                player.PopupMessageCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"));
                                return;
                            }

                            doAfterSystem.DoAfter(
                                new DoAfterEventArgs(player, MendTime, target: Owner)
                                {
                                    TargetFinishedEvent = new WiresMendedEvent()
                                    {
                                        Wire = wire,
                                        Tool = tool,
                                        User = player,
                                    },
                                    TargetCancelledEvent = new WiresCancelledEvent()
                                    {
                                        Wire = wire,
                                    },
                                    NeedHand = true,
                                });

                            PendingDoAfters.Add(wire.Id);

                            break;
                        case WiresAction.Pulse:
                            if (!tool.Qualities.Contains(PulsingQuality))
                            {
                                player.PopupMessageCursor(Loc.GetString("wires-component-ui-on-receive-message-need-wirecutters"));
                                return;
                            }

                            if (wire.IsCut)
                            {
                                player.PopupMessageCursor(Loc.GetString("wires-component-ui-on-receive-message-cannot-pulse-cut-wire"));
                                return;
                            }

                            doAfterSystem.DoAfter(
                                new DoAfterEventArgs(player, PulseTime, target: Owner)
                                {
                                    TargetFinishedEvent = new WiresPulsedEvent
                                    {
                                        Wire = wire,
                                        Tool = tool,
                                        User = player,
                                    },
                                    TargetCancelledEvent = new WiresCancelledEvent()
                                    {
                                        Wire = wire,
                                    },
                                    NeedHand = true,
                                });

                            PendingDoAfters.Add(wire.Id);

                            break;
                    }

                    break;
            }
        }

        public sealed class WiresCancelledEvent : EntityEventArgs
        {
            public Wire Wire { get; init; } = default!;
        }

        public abstract class WiresEvent : EntityEventArgs
        {
            public EntityUid User { get; init; } = default!;
            public Wire Wire { get; init; } = default!;
            public ToolComponent Tool { get; init; } = default!;
        }

        public sealed class WiresCutEvent : WiresEvent
        {
        }

        public sealed class WiresMendedEvent : WiresEvent
        {
        }

        public sealed class WiresPulsedEvent : WiresEvent
        {
        }

        internal void UpdateUserInterface()
        {
            var clientList = new List<ClientWire>();
            foreach (var entry in WiresList)
            {
                clientList.Add(new ClientWire(entry.Id, entry.IsCut, entry.Color,
                    entry.Letter));
            }

            UserInterface?.SetState(
                new WiresBoundUserInterfaceState(
                    clientList.ToArray(),
                    _statuses.Select(p => new StatusEntry(p.Key, p.Value)).ToArray(),
                    BoardName,
                    SerialNumber,
                    WireSeed));
        }

        async Task<bool> IInteractUsing.InteractUsing(InteractUsingEventArgs eventArgs)
        {
            if (!_entities.TryGetComponent<ToolComponent?>(eventArgs.Using, out var tool))
            {
                return false;
            }

            var toolSystem = EntitySystem.Get<ToolSystem>();

            // opens the wires ui if using a tool with cutting or multitool quality on it
            if (IsPanelOpen &&
               (tool.Qualities.Contains(CuttingQuality) ||
                tool.Qualities.Contains(PulsingQuality)))
            {
                if (_entities.TryGetComponent(eventArgs.User, out ActorComponent? actor))
                {
                    OpenInterface(actor.PlayerSession);
                    return true;
                }
            }

            // screws the panel open if the tool can do so
            else if (await toolSystem.UseTool(tool.Owner, eventArgs.User, Owner,
                0f, WireHackingSystem.ScrewTime, ScrewingQuality, toolComponent:tool))
            {
                IsPanelOpen = !IsPanelOpen;
                if (IsPanelOpen)
                {
                    SoundSystem.Play(Filter.Pvs(Owner), _screwdriverOpenSound.GetSound(), Owner);
                }
                else
                {
                    SoundSystem.Play(Filter.Pvs(Owner), _screwdriverCloseSound.GetSound(), Owner);
                }

                return true;
            }

            return false;
        }

        public void SetStatus(object statusIdentifier, object status)
        {
            if (_statuses.TryGetValue(statusIdentifier, out var storedMessage))
            {
                if (storedMessage == status)
                {
                    return;
                }
            }

            _statuses[statusIdentifier] = status;
            UpdateUserInterface();
        }
    }
}
