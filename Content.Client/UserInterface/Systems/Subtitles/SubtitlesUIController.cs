using System.Numerics;
using Content.Client.Gameplay;
using Content.Shared.CCVar;
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
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Subtitles;

public class CaptionEntry(LocId caption, TimeSpan lastSeen)
{
    public LocId Caption = caption;
    public TimeSpan LastSeen = lastSeen;
    public bool Left = false;
    public bool Right = false;
    public int Count = 0;

    public string DisplayText =>
        Loc.GetString("subtitle-item", ("count", Count), ("sound", Loc.GetString(Caption)), ("left", Left), ("right", Right));

    public float Opacity(TimeSpan currentTime) =>
        Math.Clamp((float)((currentTime - LastSeen) / SubtitlesUIController.CAPTION_FADEOUT), 0.0f, 1.0f);
}

[UsedImplicitly]
public sealed class SubtitlesUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<AudioSystem>
{
    private SubtitlesUI? UI => UIManager.GetActiveUIWidgetOrNull<SubtitlesUI>();
    private HashSet<EntityUid> _activeSounds = new();
    private readonly List<CaptionEntry> _captions = new();
    [UISystemDependency] private readonly CaptionTreeSystem _captionTree = default!;
    [UISystemDependency] private readonly TransformSystem _xformSystem = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public static TimeSpan CAPTION_FADEOUT = TimeSpan.FromSeconds(2.5);

    private readonly ISawmill _logMill = default!;
    private bool _captionsEnabled;

    public SubtitlesUIController() : base()
    {
        IoCManager.InjectDependencies(this);

        _logMill = _logManager.GetSawmill("captions");
        _cfg.OnValueChanged(CCVars.EnableCaptions, value => _captionsEnabled = value, true);
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
        _activeSounds.Add(entity);
    }
    private void AudioEnd(Entity<CaptionComponent, TransformComponent> entity)
    {
        _activeSounds.Remove(entity);
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (!_captionsEnabled)
        {
            return;
        }

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
        var downAngle = _eyeManager.CurrentEye.Rotation.ToWorldVec();
        var upAngle = new Vector2(downAngle.X, -downAngle.Y);
        var rightAngle = new Vector2(upAngle.Y, upAngle.X);

        var pos = mapPos.Position;
        var maxVector = new Vector2(1f, 1f);
        var worldAabb = new Box2(pos - maxVector, pos + maxVector);
        var data = _captionTree.QueryAabb(mapPos.MapId, worldAabb);

        foreach (var caption in _captions)
        {
            caption.Left = false;
            caption.Right = false;
            caption.Count = 0;
        }

        foreach (var entry in data)
        {
            if (!_activeSounds.Contains(entry.Uid))
                continue;

            var foundCaption = false;
            foreach (var caption in _captions)
            {
                if (caption.Caption != entry.Component.Caption)
                    continue;

                caption.LastSeen = _timing.CurTime;
                caption.Count++;
                foundCaption = true;

                var soundPos = _xformSystem.GetMapCoordinates(entry.Transform);
                var soundDelta = Vector2.Normalize(pos - soundPos.Position);
                var dotProduct = Vector2.Dot(upAngle, soundDelta);
                if (Math.Abs(dotProduct) >= 0.75)
                {
                    caption.Left = true;
                    caption.Right = true;
                }

                var rightDotProduct = Vector2.Dot(rightAngle, soundDelta);
                if (rightDotProduct > 0)
                    caption.Left = true;
                else
                    caption.Right = true;
                break;
            }

            if (!foundCaption && entry.Component.Caption is { } text)
                _captions.Add(new(text, _timing.CurTime));
        }

        _captions.RemoveAll(caption => caption.LastSeen + CAPTION_FADEOUT < _timing.CurTime);

        UI?.UpdateSounds(_captions, _timing.CurTime);
    }
}

