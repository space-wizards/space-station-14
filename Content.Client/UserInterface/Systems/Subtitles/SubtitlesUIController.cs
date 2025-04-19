using System.Numerics;
using JetBrains.Annotations;
using Content.Client.Gameplay;
using Robust.Client.Audio;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Components;
using System.Collections.Generic;
using Robust.Client.ComponentTrees;
using System.Linq;
using Robust.Shared.Localization;

namespace Content.Client.UserInterface.Systems.Subtitles;

[UsedImplicitly]
public sealed class SubtitlesUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<AudioSystem>
{
    private SubtitlesUI? UI => UIManager.GetActiveUIWidgetOrNull<SubtitlesUI>();
    private List<CaptionComponent> _sounds = new();
    [UISystemDependency] private readonly CaptionTreeSystem _captionTree = default!;
    [UISystemDependency] private readonly TransformSystem _xformSystem = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public void OnStateEntered(GameplayState state)
    {
    }
    public void OnStateExited(GameplayState state)
    {
    }
    public void OnSystemLoaded(AudioSystem system)
    {
        system.OnSubtitledAudioStart += AudioStart;
        system.OnSubtitledAudioEnd += AudioEnd;
    }
    public void OnSystemUnloaded(AudioSystem system)
    {
        system.OnSubtitledAudioStart -= AudioStart;
        system.OnSubtitledAudioEnd -= AudioEnd;
    }
    private void AudioStart(CaptionComponent entity)
    {
        _sounds.Add(entity);
    }
    private void AudioEnd(CaptionComponent entity)
    {
        _sounds.Remove(entity);
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        var player = _player.LocalEntity;
        if (!EntityManager.TryGetComponent(player, out TransformComponent? xform))
        {
            return;
        }
        if (_xformSystem is null || _captionTree is null)
        {
            return;
        }

        var mapPos = _xformSystem.GetMapCoordinates(xform);

        var pos = mapPos.Position;
        var maxVector = new Vector2(1f, 1f);
        var worldAabb = new Box2(pos - maxVector, pos + maxVector);
        var data = _captionTree.QueryAabb(mapPos.MapId, worldAabb);

        var groupedSounds = _sounds
            // filter to audible sounds
            .Where(a => data.Any(b => b.Component == a))
            // get the ones with non-null captions
            .Select(sound => sound.LocalizedCaption)
            .OfType<string>()
            // turn the list of sounds into a list of (sound, count)
            .GroupBy(sound => sound)
            .Select(sounds => {
                var item = sounds.Key;
                var count = sounds.Count();
                return Loc.GetString("subtitle-item", ("count", count), ("sound", item));
            })
            .ToList();

        base.FrameUpdate(args);

        UI?.UpdateSounds(groupedSounds);
    }
}

