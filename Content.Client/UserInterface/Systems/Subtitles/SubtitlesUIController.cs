using System.Linq;
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
    private List<Entity<CaptionComponent, TransformComponent>> _sounds = new();
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
        _sounds.Add(entity);
    }
    private void AudioEnd(Entity<CaptionComponent, TransformComponent> entity)
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

        var mapPos = _eyeManager.CurrentEye.Position;
        var upAngle = _eyeManager.CurrentEye.Rotation.ToVec();
        var rightAngle = new Vector2(-upAngle.Y, upAngle.X);

        var pos = mapPos.Position;
        var maxVector = new Vector2(1f, 1f);
        var worldAabb = new Box2(pos - maxVector, pos + maxVector);
        var data = _captionTree.QueryAabb(mapPos.MapId, worldAabb);

        var groupedSounds = _sounds
            // filter to audible sounds
            .Where(a => data.Any(b => b.Component == a.Comp1))
            // get the ones with non-null captions
            .Where(sound => sound.Comp1.LocalizedCaption != null)

            // turn the list of sounds into a list of (sound, count)
            .GroupBy(sound => sound.Comp1.LocalizedCaption)
            .Select(sounds => {
                var left = false;
                var right = false;

                foreach (var sound in sounds)
                {
                    var soundPos = _xformSystem.GetMapCoordinates(sound.Comp2);
                    var soundDelta = pos - soundPos.Position;
                    var dotProduct = Vector2.Dot(upAngle, soundDelta);
                    if (dotProduct <= 0.5)
                    {
                        left = true;
                        right = true;
                        break;
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

                    if (left && right)
                    {
                        break;
                    }
                }

                var item = sounds.Key;
                var count = sounds.Count();
                return Loc.GetString("subtitle-item", ("count", count), ("sound", item!), ("left", left), ("right", right));
            })
            .ToList();

        base.FrameUpdate(args);

        UI?.UpdateSounds(groupedSounds);
    }
}

