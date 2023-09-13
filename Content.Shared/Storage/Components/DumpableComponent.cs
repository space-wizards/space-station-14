using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Storage.Components;

[Serializable, NetSerializable]
public sealed partial class DumpableDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// Lets you dump this container on the ground using a verb,
/// or when interacting with it on a disposal unit or placeable surface.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DumpableComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("soundDump"), AutoNetworkedField]
    public SoundSpecifier? DumpSound = new SoundCollectionSpecifier("storageRustle");

    /// <summary>
    /// How long each item adds to the doafter.
    /// </summary>
    [DataField("delayPerItem"), AutoNetworkedField]
    public TimeSpan DelayPerItem = TimeSpan.FromSeconds(0.2);

    /// <summary>
    /// The multiplier modifier
    /// </summary>
    [DataField("multiplier"), AutoNetworkedField]
    public float Multiplier = 1.0f;
}
