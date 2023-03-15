using Content.Shared.Mech.Equipment.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Mech.Equipment.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMechSoundboardSystem))]
public sealed class MechSoundboardComponent : Component
{
    /// <summary>
    /// List of sounds that can be played
    /// </summary>
    [DataField("sounds"), ViewVariables(VVAccess.ReadWrite)]
    public List<SoundCollectionSpecifier> Sounds = new();
}

[Serializable]
public sealed class MechSoundboardComponentState : ComponentState
{
    public List<SoundCollectionSpecifier> Sounds;

    public MechSoundboardComponentState(List<SoundCollectionSpecifier> sounds)
    {
        Sounds = sounds;
    }
}
