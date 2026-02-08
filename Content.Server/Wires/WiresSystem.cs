using Content.Server.Construction;
using Content.Server.Construction.Components;
using Content.Shared.Wires;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Wires;

public sealed partial class WiresSystem : SharedWiresSystem
{
    [Dependency] private readonly ConstructionSystem _construction = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WiresComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<WiresComponent> ent, ref MapInitEvent args)
    {
        if (!string.IsNullOrEmpty(ent.Comp.LayoutId))
            SetOrCreateWireLayout(ent.AsNullable());

        if (ent.Comp.SerialNumber == null)
            GenerateSerialNumber(ent.AsNullable());

        if (ent.Comp.WireSeed == 0)
            ent.Comp.WireSeed = _random.Next(1, int.MaxValue);

        // Update the construction graph to make sure that it starts on the node specified by WiresPanelSecurityComponent
        if (TryComp<WiresPanelSecurityComponent>(ent.Owner, out var wiresPanelSecurity) &&
            !string.IsNullOrEmpty(wiresPanelSecurity.SecurityLevel) &&
            TryComp<ConstructionComponent>(ent.Owner, out var construction))
        {
            _construction.ChangeNode(ent.Owner, null, wiresPanelSecurity.SecurityLevel, true, construction);
        }

        UpdateUserInterface(ent.Owner);
    }

    private void SetOrCreateWireLayout(Entity<WiresComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        WireLayout? layout = null;
        List<Wire>? wireSet = null;
        if (!ent.Comp.AlwaysRandomize)
            TryGetLayout(ent.Comp.LayoutId, out layout);

        List<IWireAction> wireActions = [];
        var dummyWires = 0;

        if (!_protoMan.Resolve(ent.Comp.LayoutId, out var layoutPrototype))
            return;

        dummyWires += layoutPrototype.DummyWires;

        if (layoutPrototype.Wires != null)
            wireActions.AddRange(layoutPrototype.Wires);

        // does the prototype have a parent (and are the wires empty?) if so, we just create
        // a new layout based on that
        foreach (var parentLayout in _protoMan.EnumerateParents<WireLayoutPrototype>(ent.Comp.LayoutId))
        {
            if (parentLayout.Wires != null)
            {
                wireActions.AddRange(parentLayout.Wires);
            }

            dummyWires += parentLayout.DummyWires;
        }

        if (wireActions.Count > 0)
        {
            foreach (var wire in wireActions)
            {
                wire.Initialize();
            }

            wireSet = CreateWireSet(ent.Owner, layout, wireActions, dummyWires);
        }

        if (wireSet == null || wireSet.Count == 0)
            return;

        ent.Comp.WiresList.AddRange(wireSet);

        var types = new Dictionary<object, int>();

        if (layout != null)
        {
            for (var i = 0; i < wireSet.Count; i++)
            {
                ent.Comp.WiresList[layout.Specifications[i].Position] = wireSet[i];
            }

            var id = 0;
            foreach (var wire in ent.Comp.WiresList)
            {
                wire.Id = id++;
                if (wire.Action == null)
                    continue;

                var wireType = wire.Action.GetType();
                if (types.ContainsKey(wireType))
                {
                    types[wireType] += 1;
                }
                else
                {
                    types.Add(wireType, 1);
                }

                // don't care about the result, this should've
                // been handled in layout creation
                wire.Action.AddWire(wire, types[wireType]);
            }
        }
        else
        {
            var enumeratedList = new List<(int, Wire)>();
            var data = new Dictionary<int, WireLayout.WireData>();
            for (var i = 0; i < wireSet.Count; i++)
            {
                enumeratedList.Add((i, wireSet[i]));
            }
            _random.Shuffle(enumeratedList);

            for (var i = 0; i < enumeratedList.Count; i++)
            {
                var (id, d) = enumeratedList[i];
                d.Id = i;

                if (d.Action != null)
                {
                    var actionType = d.Action.GetType();
                    if (!types.TryAdd(actionType, 1))
                        types[actionType] += 1;

                    if (!d.Action.AddWire(d, types[actionType]))
                        d.Action = null;
                }

                data.Add(id, new WireLayout.WireData(d.Letter, d.Color, i));
                ent.Comp.WiresList[i] = wireSet[id];
            }

            if (!ent.Comp.AlwaysRandomize && !string.IsNullOrEmpty(ent.Comp.LayoutId))
                AddLayout(ent.Comp.LayoutId, new WireLayout(data));
        }

        Dirty(ent);
    }

    private void GenerateSerialNumber(Entity<WiresComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        Span<char> data = stackalloc char[9];
        data[4] = '-';

        if (_random.Prob(0.01f))
        {
            for (var i = 0; i < 4; i++)
            {
                // Cyrillic Letters
                data[i] = (char)_random.Next(0x0410, 0x0430);
            }
        }
        else
        {
            for (var i = 0; i < 4; i++)
            {
                // Letters
                data[i] = (char)_random.Next(0x41, 0x5B);
            }
        }

        for (var i = 5; i < 9; i++)
        {
            // Digits
            data[i] = (char)_random.Next(0x30, 0x3A);
        }

        ent.Comp.SerialNumber = new string(data);
        Dirty(ent);
        UpdateUserInterface(ent.Owner);
    }

    private List<Wire>? CreateWireSet(EntityUid uid, WireLayout? layout, List<IWireAction> wires, int dummyWires)
    {
        if (wires.Count == 0)
            return null;

        var colors = new List<WireColor>(Enum.GetValues<WireColor>());
        var letters = new List<WireLetter>(Enum.GetValues<WireLetter>());

        var wireSet = new List<Wire>();
        for (var i = 0; i < wires.Count; i++)
        {
            wireSet.Add(CreateWire(uid, wires[i], i, layout, colors, letters));
        }

        for (var i = 1; i <= dummyWires; i++)
        {
            wireSet.Add(CreateWire(uid, null, wires.Count + i, layout, colors, letters));
        }

        return wireSet;
    }

    private Wire CreateWire(EntityUid uid, IWireAction? action, int position, WireLayout? layout, List<WireColor> colors, List<WireLetter> letters)
    {
        WireLetter letter;
        WireColor color;

        if (layout != null
            && layout.Specifications.TryGetValue(position, out var spec))
        {
            color = spec.Color;
            letter = spec.Letter;
            colors.Remove(color);
            letters.Remove(letter);
        }
        else
        {
            color = colors.Count == 0 ? WireColor.Red : _random.PickAndTake(colors);
            letter = letters.Count == 0 ? WireLetter.α : _random.PickAndTake(letters);
        }

        return new Wire(
            uid,
            false,
            color,
            letter,
            position,
            action);
    }
}
