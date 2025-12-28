using System.Numerics;
using Content.Shared.DoAfter;
using Content.Shared.Mech.Equipment.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Equipment.Components;

/// <summary>
/// A piece of mech equipment that grabs entities and stores them
/// inside of a container so large objects can be moved.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MechGrabberSystem))]
public sealed partial class MechGrabberComponent : Component
{
    /// <summary>
    /// The change in energy after each grab.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GrabEnergyDelta = -30;

    /// <summary>
    /// How long does it take to grab something?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float GrabDelay = 2.5f;

    /// <summary>
    /// The offset from the mech when an item is dropped.
    /// This is here for things like lockers and vendors.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 DepositOffset = new(0, -1);

    /// <summary>
    /// The maximum amount of items that can be fit in this grabber.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxContents = 10;

    /// <summary>
    /// The sound played when a mech is grabbing something.
    /// </summary>
    [DataField]
    public SoundSpecifier GrabSound = new SoundPathSpecifier("/Audio/Mecha/sound_mecha_hydraulic.ogg");

    public EntityUid? AudioStream;

    [ViewVariables(VVAccess.ReadWrite)]
    public Container ItemContainer = new();

    [NonSerialized, ViewVariables(VVAccess.ReadOnly)]
    public DoAfterId? DoAfter;
}
