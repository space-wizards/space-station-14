using Content.Server.DeviceLinking.Components;
using Content.Shared.DeviceLinking;
using Content.Server.Storage.Components;
using Content.Shared.Placeable;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.DeviceLinking.Systems;

/// <see cref="PressurePlateComponent"/>
public sealed class PressurePlateSystem : EntitySystem
{
    [Dependency] private readonly DeviceLinkSystem _signalSystem = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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
            totalMass += GetEntWeightRecursive(ent);
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

    /// <summary>
    /// Recursively calculates the weight of the object, and all its contents, and the contents and its contents...
    /// </summary>
    private float GetEntWeightRecursive(EntityUid uid)
    {
        var totalMass = 0f;
        if (Deleted(uid)) return 0f;
        if (TryComp<PhysicsComponent>(uid, out var physics))
        {
            totalMass += physics.Mass;
        }
        if (TryComp<EntityStorageComponent>(uid, out var entityStorage))
        {
            var storage = entityStorage.Contents;
            foreach (var ent in storage.ContainedEntities)
            {
                totalMass += GetEntWeightRecursive(ent);
            }
        }
        return totalMass;
    }
}
