using System;
using System.Collections.Generic;
using Content.Server.Tools;
using Content.Server.VendingMachines;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.ViewVariables;
using static Content.Shared.Wires.SharedWiresComponent;

namespace Content.Server.WireHacking
{
    public sealed class WireHackingSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly ToolSystem _tools = default!;

        [ViewVariables] private readonly Dictionary<string, WireLayout> _layouts =
            new();

        public const float ScrewTime = 2.5f;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<WiresComponent, ComponentStartup>(OnWiresStartup);
            SubscribeLocalEvent<WiresComponent, MapInitEvent>(OnWiresMapInit);
            SubscribeLocalEvent<WiresComponent, ExaminedEvent>(OnWiresExamine);

            // Hacking DoAfters
            SubscribeLocalEvent<WiresComponent, WiresComponent.WiresCutEvent>(OnWiresCut);
            SubscribeLocalEvent<WiresComponent, WiresComponent.WiresMendedEvent>(OnWiresMended);
            SubscribeLocalEvent<WiresComponent, WiresComponent.WiresPulsedEvent>(OnWiresPulsed);
            SubscribeLocalEvent<WiresComponent, WiresComponent.WiresCancelledEvent>(OnWiresCancelled);

            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);
        }

        private void OnWiresCancelled(EntityUid uid, WiresComponent component, WiresComponent.WiresCancelledEvent args)
        {
            component.PendingDoAfters.Remove(args.Wire.Id);
        }

        private void HackingInteract(WiresComponent component, WiresComponent.Wire wire)
        {
            component.PendingDoAfters.Remove(wire.Id);
        }

        private void OnWiresCut(EntityUid uid, WiresComponent component, WiresComponent.WiresCutEvent args)
        {
            HackingInteract(component, args.Wire);

            // Re-validate
            // Deletion for user + wires should already be handled by do-after and tool is checked once at end in active-hand anyway.
            if (!component.CanWiresInteract(args.User, out var tool)) return;

            if (!tool.Qualities.Contains(component.CuttingQuality)) return;

            var wire = args.Wire;

            _tools.PlayToolSound(args.Tool.Owner, args.Tool);
            wire.IsCut = true;
            component.UpdateUserInterface();

            wire.Owner.WiresUpdate(new WiresUpdateEventArgs(wire.Identifier, WiresAction.Cut));
        }

        private void OnWiresMended(EntityUid uid, WiresComponent component, WiresComponent.WiresMendedEvent args)
        {
            HackingInteract(component, args.Wire);

            if (!component.CanWiresInteract(args.User, out var tool)) return;

            if (!tool.Qualities.Contains(component.CuttingQuality)) return;

            var wire = args.Wire;

            _tools.PlayToolSound(args.Tool.Owner, args.Tool);
            wire.IsCut = false;
            component.UpdateUserInterface();

            wire.Owner.WiresUpdate(new WiresUpdateEventArgs(wire.Identifier, WiresAction.Mend));
        }

        private void OnWiresPulsed(EntityUid uid, WiresComponent component, WiresComponent.WiresPulsedEvent args)
        {
            HackingInteract(component, args.Wire);

            if (args.Wire.IsCut || !component.CanWiresInteract(args.User, out var tool)) return;

            if (!tool.Qualities.Contains(component.PulsingQuality)) return;

            var wire = args.Wire;
            SoundSystem.Play(Filter.Pvs(uid), component.PulseSound.GetSound(), uid);
            wire.Owner.WiresUpdate(new WiresUpdateEventArgs(wire.Identifier, WiresAction.Pulse));
        }

        private void OnWiresExamine(EntityUid uid, WiresComponent component, ExaminedEvent args)
        {
            args.PushMarkup(Loc.GetString(component.IsPanelOpen
                ? "wires-component-on-examine-panel-open"
                : "wires-component-on-examine-panel-closed"));
        }

        private void OnWiresStartup(EntityUid uid, WiresComponent component, ComponentStartup args)
        {
            WireLayout? layout = null;
            if (component.LayoutId != null)
            {
                _layouts.TryGetValue(component.LayoutId, out layout);
            }

            foreach (var wiresProvider in EntityManager.GetComponents<IWires>(uid))
            {
                var builder = new WiresComponent.WiresBuilder(component, wiresProvider, layout);
                wiresProvider.RegisterWires(builder);
            }

            if (layout != null)
            {
                component.WiresList.Sort((a, b) =>
                {
                    var pA = layout.Specifications[a.Identifier].Position;
                    var pB = layout.Specifications[b.Identifier].Position;

                    return pA.CompareTo(pB);
                });
            }
            else
            {
                _random.Shuffle(component.WiresList);

                if (component.LayoutId != null)
                {
                    var dict = new Dictionary<object, WireLayout.WireData>();
                    for (var i = 0; i < component.WiresList.Count; i++)
                    {
                        var d = component.WiresList[i];
                        dict.Add(d.Identifier, new WireLayout.WireData(d.Letter, d.Color, i));
                    }

                    _layouts.Add(component.LayoutId, new WireLayout(dict));
                }
            }

            var id = 0;
            foreach (var wire in component.WiresList)
            {
                wire.Id = ++id;
            }

            component.UpdateUserInterface();
        }

        private void Reset(RoundRestartCleanupEvent ev)
        {
            _layouts.Clear();
        }

        private void OnWiresMapInit(EntityUid uid, WiresComponent component, MapInitEvent args)
        {
            if (component.SerialNumber == null)
            {
                GenerateSerialNumber(component);
            }

            if (component.WireSeed == 0)
            {
                component.WireSeed = _random.Next(1, int.MaxValue);
                component.UpdateUserInterface();
            }
        }

        private void GenerateSerialNumber(WiresComponent component)
        {
            Span<char> data = stackalloc char[9];
            data[4] = '-';

            if (_random.Prob(0.01f))
            {
                for (var i = 0; i < 4; i++)
                {
                    // Cyrillic Letters
                    data[i] = (char) _random.Next(0x0410, 0x0430);
                }
            }
            else
            {
                for (var i = 0; i < 4; i++)
                {
                    // Letters
                    data[i] = (char) _random.Next(0x41, 0x5B);
                }
            }

            for (var i = 5; i < 9; i++)
            {
                // Digits
                data[i] = (char) _random.Next(0x30, 0x3A);
            }

            component.SerialNumber = new string(data);
        }
    }

    public sealed class WireLayout
    {
        [ViewVariables] public IReadOnlyDictionary<object, WireData> Specifications { get; }

        public WireLayout(IReadOnlyDictionary<object, WireData> specifications)
        {
            Specifications = specifications;
        }

        public sealed class WireData
        {
            public WireLetter Letter { get; }
            public WireColor Color { get; }
            public int Position { get; }

            public WireData(WireLetter letter, WireColor color, int position)
            {
                Letter = letter;
                Color = color;
                Position = position;
            }
        }
    }
}
