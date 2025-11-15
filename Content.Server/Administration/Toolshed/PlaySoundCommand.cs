using System.Linq;
using Content.Shared.Administration;
using Content.Shared.Coordinates;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server.Administration.Toolshed;

/// <summary>
/// Toolshed version of PlayLocalSound
/// </summary>
[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class PlaySoundCommand : ToolshedCommand
{
    private SharedAudioSystem? _audioSystem;

    [CommandImplementation("at")]
    public IEnumerable<EntityUid?> PlaySoundAt([PipedArgument] IEnumerable<EntityCoordinates> targets,
        ResPath resPath,
        float volume = 0)
    {
        return targets.Select(x =>
        {
            _audioSystem ??= GetSys<SharedAudioSystem>();

            var audioParams = AudioParams.Default.WithVolume(volume);
            var sound = new SoundPathSpecifier(resPath, audioParams);
            return _audioSystem.PlayPvs(sound, x)?.Entity;
        });
    }

    [CommandImplementation("on")]
    public IEnumerable<EntityUid?> PlaySoundOn([PipedArgument] IEnumerable<EntityUid> targets,
        ResPath resPath,
        float volume = 0)
    {
        return PlaySoundAt(targets.Select(x => Transform(x).Coordinates), resPath, volume);
    }

    [CommandImplementation("attached")]
    public IEnumerable<EntityUid?> PlaySoundAttached([PipedArgument] IEnumerable<EntityUid> targets,
        ResPath resPath,
        float volume = 0)
    {
        return PlaySoundAt(targets.Select(x => x.ToCoordinates()), resPath, volume);
    }
}
