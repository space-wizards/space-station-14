using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CosmicFinaleComponent : Component
{
    [DataField]
    public FinaleState CurrentState = FinaleState.Unavailable;

    [DataField]
    public bool FinaleDelayStarted = false;

    [DataField]
    public bool FinaleActive = false;

    [DataField]
    public bool Occupied = false;

    [DataField]
    public bool MusicBool = false;

    [AutoPausedField] public TimeSpan FinaleTimer = default!;
    [AutoPausedField] public TimeSpan BufferTimer = default!;
    [AutoPausedField] public TimeSpan CultistsCheckTimer = default!;
    [DataField, AutoNetworkedField] public TimeSpan BufferRemainingTime = TimeSpan.FromSeconds(360);
    [DataField, AutoNetworkedField] public TimeSpan FinaleRemainingTime = TimeSpan.FromSeconds(125);
    [DataField, AutoNetworkedField] public TimeSpan CheckWait = TimeSpan.FromSeconds(5);
    [DataField] public SoundSpecifier CancelEventSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");
    [DataField] public TimeSpan FinaleSongLength;
    [DataField] public TimeSpan SongLength;
    [DataField] public SoundSpecifier? SelectedSong;
    [DataField] public TimeSpan InteractionTime = TimeSpan.FromSeconds(15);
    [DataField] public SoundSpecifier BufferMusic = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/premonition.ogg");
    [DataField] public SoundSpecifier FinaleMusic = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/a_new_dawn.ogg");

    /// <summary>
    /// The degen that people suffer if they don't have mindshields, aren't a chaplain, or aren't cultists while the Finale is Available or Active
    /// </summary>
    [DataField]
    public DamageSpecifier FinaleDegen = new()
    {
        DamageDict = new()
        {
            { "Blunt", 2.25},
            { "Cold", 2.25},
            { "Radiation", 2.25},
            { "Asphyxiation", 2.25}
        }
    };
}

[Serializable]
public enum FinaleState : byte
{
    Unavailable,
    ReadyBuffer,
    ReadyFinale,
    ActiveBuffer,
    ActiveFinale,
    Victory,
}
