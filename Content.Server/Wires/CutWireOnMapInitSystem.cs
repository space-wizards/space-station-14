using Robust.Shared.Random;

namespace Content.Server.Wires;

/// <summary>
/// Handles cutting a random wire on devices that have <see cref="CutWireOnMapInitComponent"/>.
/// </summary>
public sealed partial class CutWireOnMapInitSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CutWireOnMapInitComponent, MapInitEvent>(OnMapInit, after: [typeof(WiresSystem)]);
    }

    private void OnMapInit(Entity<CutWireOnMapInitComponent> entity, ref MapInitEvent args)
    {
        if (TryComp<WiresComponent>(entity, out var panel) && panel.WiresList.Count > 0)
        {
            // Pick a random wire
            var targetWire = _random.Pick(panel.WiresList);

            // Cut the wire
            if (targetWire.Action == null || targetWire.Action.Cut(EntityUid.Invalid, targetWire))
                targetWire.IsCut = true;
        }

        // Our work here is done
        RemCompDeferred(entity, entity.Comp);
    }
}
