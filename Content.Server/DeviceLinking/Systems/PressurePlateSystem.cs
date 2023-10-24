using Content.Server.DeviceLinking.Components;
using Content.Server.Physics;
using Content.Shared.DeviceLinking;
using Content.Shared.Placeable;

namespace Content.Server.DeviceLinking.Systems;

/// <see cref="PressurePlateComponent"/>
public sealed class PressurePlateSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly WeightSystem _weight = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PressurePlateComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<PressurePlateComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<PressurePlateComponent, ItemRemovedEvent>(OnItemRemoved);
    }

    private void OnItemRemoved(EntityUid uid, PressurePlateComponent component, ref ItemRemovedEvent args)
    {
        UpdateState(uid, component);
    }

    private void OnItemPlaced(EntityUid uid, PressurePlateComponent component, ref ItemPlacedEvent args)
    {
        UpdateState(uid, component);
    }

    private void OnInit(EntityUid uid, PressurePlateComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSourcePorts(uid, component.PressedPort, component.ReleasedPort);
        _appearance.SetData(uid, PressurePlateVisuals.Pressed, false);
    }
    private void UpdateState(EntityUid uid, PressurePlateComponent component)
    {
        if (!TryComp<ItemPlacerComponent>(uid, out var itemPlacer))
            return;

        var totalMass = 0f;
        foreach (var ent in itemPlacer.PlacedEntities)
        {
            totalMass += _weight.GetEntWeightRecursive(ent);
        }
        var pressed = totalMass >= component.WeightRequired;
        if (pressed == component.IsPressed)
            return;

        component.IsPressed = pressed;
        _signalSystem.SendSignal(uid, component.StatusPort, pressed);
        _signalSystem.InvokePort(uid, pressed ? component.PressedPort : component.ReleasedPort);

        _appearance.SetData(uid, PressurePlateVisuals.Pressed, pressed);
        _audio.PlayPvs(pressed ? component.PressedSound : component.ReleasedSound, uid);
    }
}
