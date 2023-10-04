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
        SubscribeLocalEvent<PressurePlateComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<PressurePlateComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnInit(EntityUid uid, PressurePlateComponent component, ComponentInit args)
    {
        _signalSystem.EnsureSourcePorts(uid, component.PressedPort, component.ReleasedPort);
        _appearance.SetData(uid, PressurePlateVisuals.Pressed, false);
    }
    private void OnStartCollide(EntityUid uid, PressurePlateComponent component, ref StartCollideEvent args)
    {
        UpdateState(uid, component);
    }

    private void OnEndCollide(EntityUid uid, PressurePlateComponent component, ref EndCollideEvent args)
    {
        UpdateState(uid, component);
    }

    private void UpdateState(EntityUid uid, PressurePlateComponent component)
    {
        if (!TryComp(uid, out ItemPlacerComponent? itemPlacer))
            return;


        var totalMass = 0f;
        foreach (var ent in itemPlacer.PlacedEntities)
        {
            totalMass += GetEntWeightRecursive(ent);
        }

        if (component.isPressed == true && totalMass < component.WeightRequired) //Release
        {
            component.isPressed = false;
            _signalSystem.InvokePort(uid, component.TogglePort);
            _signalSystem.InvokePort(uid, component.ReleasedPort);
            _appearance.SetData(uid, PressurePlateVisuals.Pressed, false);
            _audio.PlayPvs(component.ReleasedSound, uid);
        }

        if (component.isPressed == false && totalMass > component.WeightRequired) //Press
        {
            component.isPressed = true;
            _signalSystem.InvokePort(uid, component.TogglePort);
            _signalSystem.InvokePort(uid, component.PressedPort);
            _appearance.SetData(uid, PressurePlateVisuals.Pressed, true);
            _audio.PlayPvs(component.PressedSound, uid);
        }
        Dirty(uid, component);
    }

    /// <summary>
    /// Recursively calculates the weight of the object, and all its contents, and the contents and its contents...
    /// </summary>
    private float GetEntWeightRecursive(EntityUid uid)
    {
        var totalMass = 0f;
        if (Deleted(uid)) return 0f;
        if (TryComp(uid, out PhysicsComponent? physics))
        {
            totalMass += physics.Mass;
        }
        if (TryComp(uid, out EntityStorageComponent? entityStorage))
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

