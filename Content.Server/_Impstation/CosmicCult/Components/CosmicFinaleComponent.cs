using Robust.Shared.Audio;

namespace Content.Server._Impstation.CosmicCult.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CosmicFinaleComponent : Component
{
    [DataField] public bool FinaleReady = false;
    [DataField] public bool FinaleActive = false;
    [DataField] public bool Victory = false;
    [DataField] public bool Occupied = false;
    [DataField] public bool PlayedFinaleSong = false;
    [DataField] public bool PlayedBufferSong = false;
    [DataField] public bool BufferComplete = false;
    [AutoPausedField] public TimeSpan FinaleTimer = default!;
    [AutoPausedField] public TimeSpan BufferTimer = default!;
    [AutoPausedField] public TimeSpan CultistsCheckTimer = default!;
    [DataField, AutoNetworkedField] public TimeSpan BufferRemainingTime = TimeSpan.FromSeconds(360);
    [DataField, AutoNetworkedField] public TimeSpan FinaleRemainingTime = TimeSpan.FromSeconds(125);
    [DataField, AutoNetworkedField] public TimeSpan CheckWait = TimeSpan.FromSeconds(5);
    [DataField] public SoundSpecifier CancelEventSound = new SoundPathSpecifier("/Audio/Misc/notice2.ogg");
    [DataField] public TimeSpan FinaleSongLength;
    [DataField] public TimeSpan SongLength;
    [DataField] public string SelectedFinaleSong = string.Empty;
    [DataField] public string SelectedBufferSong = string.Empty;
    [DataField] public TimeSpan InteractionTime = TimeSpan.FromSeconds(15);
    [DataField] public SoundSpecifier BufferMusic = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/Before Dawn.ogg");
    [DataField] public SoundSpecifier FinaleMusic = new SoundPathSpecifier("/Audio/_Impstation/CosmicCult/A New Dawn.ogg", AudioParams.Default.AddVolume(+12f));
}
