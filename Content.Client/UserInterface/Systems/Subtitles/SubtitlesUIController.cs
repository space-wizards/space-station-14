using System.Numerics;
using Content.Client.Gameplay;
using JetBrains.Annotations;
using Robust.Client.Audio;
using Robust.Client.ComponentTrees;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Subtitles;

[UsedImplicitly]
public sealed class SubtitlesUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<AudioSystem>
{
    private SubtitlesUI? UI => UIManager.GetActiveUIWidgetOrNull<SubtitlesUI>();
    private Dictionary<string, List<Entity<CaptionComponent, TransformComponent>>> _sounds = new();
    private readonly List<string> _soundStrings = new();
    [UISystemDependency] private readonly CaptionTreeSystem _captionTree = default!;
    [UISystemDependency] private readonly TransformSystem _xformSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private readonly ISawmill _logMill = default!;

    public SubtitlesUIController() : base()
    {
        IoCManager.InjectDependencies(this);

        _logMill = _logManager.GetSawmill("captions");
    }

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
    private void AudioStart(Entity<CaptionComponent, TransformComponent> entity)
    {
        if (entity.Comp1.LocalizedCaption is not {} caption)
            return;

        if (!_sounds.TryGetValue(caption, out var items))
        {
            items = new();
            _sounds[caption] = items;
        }
        _sounds[caption].Add(entity);
    }
    private void AudioEnd(Entity<CaptionComponent, TransformComponent> entity)
    {
        if (entity.Comp1.LocalizedCaption is not {} caption)
            return;

        if (_sounds.TryGetValue(caption, out var items))
        {
            items.Remove(entity);
            if (items.Count == 0)
            {
                _sounds.Remove(caption);
            }
        }
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var player = _player.LocalEntity;
        if (!EntityManager.TryGetComponent(player, out TransformComponent? xform))
        {
            return;
        }
        if (_xformSystem is null || _captionTree is null)
        {
            return;
        }

        var mapPos = _eyeManager.CurrentEye.Position;
        var upAngle = _eyeManager.CurrentEye.Rotation.ToVec();
        var rightAngle = new Vector2(-upAngle.Y, upAngle.X);

        var pos = mapPos.Position;
        var maxVector = new Vector2(1f, 1f);
        var worldAabb = new Box2(pos - maxVector, pos + maxVector);
        var data = _captionTree.QueryAabb(mapPos.MapId, worldAabb);

        _soundStrings.Clear();

        foreach (var (caption, sounds) in _sounds)
        {
            var left = false;
            var right = false;
            int countedSounds = 0;

            foreach (var sound in sounds)
            {
                if (!data.Contains((sound.Comp1, sound.Comp2)))
                    continue;

                countedSounds++;

                var soundPos = _xformSystem.GetMapCoordinates(sound.Comp2);
                var soundDelta = pos - soundPos.Position;
                var dotProduct = Vector2.Dot(upAngle, soundDelta);
                if (dotProduct <= 0.5)
                {
                    left = true;
                    right = true;
                }
                var rightDotProduct = Vector2.Dot(rightAngle, soundDelta);
                if (rightDotProduct > 0)
                {
                    right = true;
                }
                else
                {
                    left = true;
                }
            }

            if (!left && !right)
            {
                continue;
            }

            _soundStrings.Add(Loc.GetString("subtitle-item", ("count", countedSounds), ("sound", caption), ("left", left), ("right", right)));
        }

        UI?.UpdateSounds(_soundStrings);
    }
}

