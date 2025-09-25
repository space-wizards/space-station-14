using System.Numerics;
using Content.Client.Message;
using Content.Client.Paper.UI;
using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Content.Shared.Tips;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using static Content.Client.Tips.TippyUI;

namespace Content.Client.Tips;

public sealed class TippyUIController : UIController
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [UISystemDependency] private readonly AudioSystem _audio = default!;
    [UISystemDependency] private readonly SpriteSystem _sprite = default!;

    public const float Padding = 50;
    public static Angle WaddleRotation = Angle.FromDegrees(10);

    private EntityUid _entity;
    private float _secondsUntilNextState;
    private int _previousStep = 0;
    private TippyEvent? _currentMessage;
    private readonly Queue<TippyEvent> _queuedMessages = new();

    public override void Initialize()
    {
        base.Initialize();
        UIManager.OnScreenChanged += OnScreenChanged;
        SubscribeNetworkEvent<TippyEvent>(OnTippyEvent);
    }

    private void OnTippyEvent(TippyEvent msg, EntitySessionEventArgs args)
    {
        _queuedMessages.Enqueue(msg);
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        var screen = UIManager.ActiveScreen;
        if (screen == null)
        {
            _queuedMessages.Clear();
            return;
        }

        var tippy = screen.GetOrAddWidget<TippyUI>();
        _secondsUntilNextState -= args.DeltaSeconds;

        if (_secondsUntilNextState <= 0)
            NextState(tippy);
        else
        {
            var pos = UpdatePosition(tippy, screen.Size, args); ;
            LayoutContainer.SetPosition(tippy, pos);
        }
    }

    private Vector2 UpdatePosition(TippyUI tippy, Vector2 screenSize, FrameEventArgs args)
    {
        if (_currentMessage == null)
            return default;

        var slideTime = _currentMessage.SlideTime;

        var offset = tippy.State switch
        {
            TippyState.Hidden => 0,
            TippyState.Revealing => Math.Clamp(1 - _secondsUntilNextState / slideTime, 0, 1),
            TippyState.Hiding => Math.Clamp(_secondsUntilNextState / slideTime, 0, 1),
            _ => 1,
        };

        var waddle = _currentMessage.WaddleInterval;

        if (_currentMessage == null
            || waddle <= 0
            || tippy.State == TippyState.Hidden
            || tippy.State == TippyState.Speaking
            || !EntityManager.TryGetComponent(_entity, out SpriteComponent? sprite))
        {
            return new Vector2(screenSize.X - offset * (tippy.DesiredSize.X + Padding), (screenSize.Y - tippy.DesiredSize.Y) / 2);
        }

        var numSteps = (int)Math.Ceiling(slideTime / waddle);
        var curStep = (int)Math.Floor(numSteps * offset);
        var stepSize = (tippy.DesiredSize.X + Padding) / numSteps;

        if (curStep != _previousStep)
        {
            _previousStep = curStep;
            _sprite.SetRotation((_entity, sprite),
                sprite.Rotation > 0
                    ? -WaddleRotation
                    : WaddleRotation);

            if (EntityManager.TryGetComponent(_entity, out FootstepModifierComponent? step) && step.FootstepSoundCollection != null)
            {
                var audioParams = step.FootstepSoundCollection.Params
                    .AddVolume(-7f)
                    .WithVariation(0.1f);
                _audio.PlayGlobal(step.FootstepSoundCollection, EntityUid.Invalid, audioParams);
            }
        }

        return new Vector2(screenSize.X - stepSize * curStep, (screenSize.Y - tippy.DesiredSize.Y) / 2);
    }

    private void NextState(TippyUI tippy)
    {
        SpriteComponent? sprite;
        switch (tippy.State)
        {
            case TippyState.Hidden:
                if (!_queuedMessages.TryDequeue(out var next))
                    return;

                if (next.Proto != null)
                {
                    _entity = EntityManager.SpawnEntity(next.Proto, MapCoordinates.Nullspace);
                    tippy.ModifyLayers = false;
                }
                else
                {
                    _entity = EntityManager.SpawnEntity(_cfg.GetCVar(CCVars.TippyEntity), MapCoordinates.Nullspace);
                    tippy.ModifyLayers = true;
                }
                if (!EntityManager.TryGetComponent(_entity, out sprite))
                    return;
                if (!EntityManager.HasComponent<PaperVisualsComponent>(_entity))
                {
                    var paper = EntityManager.AddComponent<PaperVisualsComponent>(_entity);
                    paper.BackgroundImagePath = "/Textures/Interface/Paper/paper_background_default.svg.96dpi.png";
                    paper.BackgroundPatchMargin = new(16f, 16f, 16f, 16f);
                    paper.BackgroundModulate = new(255, 255, 204);
                    paper.FontAccentColor = new(0, 0, 0);
                }
                tippy.InitLabel(EntityManager.GetComponentOrNull<PaperVisualsComponent>(_entity), _resCache);

                var scale = sprite.Scale;
                if (tippy.ModifyLayers)
                {
                    _sprite.SetScale((_entity, sprite), Vector2.One);
                }
                else
                {
                    _sprite.SetScale((_entity, sprite), new Vector2(3, 3));
                }
                tippy.Entity.SetEntity(_entity);
                tippy.Entity.Scale = scale;

                _currentMessage = next;
                _secondsUntilNextState = next.SlideTime;
                tippy.State = TippyState.Revealing;
                _previousStep = 0;
                if (tippy.ModifyLayers)
                {
                    _sprite.LayerSetAnimationTime((_entity, sprite), "revealing", 0);
                    _sprite.LayerSetVisible((_entity, sprite), "revealing", true);
                    _sprite.LayerSetVisible((_entity, sprite), "speaking", false);
                    _sprite.LayerSetVisible((_entity, sprite), "hiding", false);
                }
                _sprite.SetRotation((_entity, sprite), 0);
                tippy.Label.SetMarkupPermissive(_currentMessage.Msg);
                tippy.Label.Visible = false;
                tippy.LabelPanel.Visible = false;
                tippy.Visible = true;
                _sprite.SetVisible((_entity, sprite), true);
                break;

            case TippyState.Revealing:
                tippy.State = TippyState.Speaking;
                if (!EntityManager.TryGetComponent(_entity, out sprite))
                    return;
                _sprite.SetRotation((_entity, sprite), 0);
                _previousStep = 0;
                if (tippy.ModifyLayers)
                {
                    _sprite.LayerSetAnimationTime((_entity, sprite), "speaking", 0);
                    _sprite.LayerSetVisible((_entity, sprite), "revealing", false);
                    _sprite.LayerSetVisible((_entity, sprite), "speaking", true);
                    _sprite.LayerSetVisible((_entity, sprite), "hiding", false);
                }
                tippy.Label.Visible = true;
                tippy.LabelPanel.Visible = true;
                tippy.InvalidateArrange();
                tippy.InvalidateMeasure();
                if (_currentMessage != null)
                    _secondsUntilNextState = _currentMessage.SpeakTime;

                break;

            case TippyState.Speaking:
                tippy.State = TippyState.Hiding;
                if (!EntityManager.TryGetComponent(_entity, out sprite))
                    return;
                if (tippy.ModifyLayers)
                {
                    _sprite.LayerSetAnimationTime((_entity, sprite), "hiding", 0);
                    _sprite.LayerSetVisible((_entity, sprite), "revealing", false);
                    _sprite.LayerSetVisible((_entity, sprite), "speaking", false);
                    _sprite.LayerSetVisible((_entity, sprite), "hiding", true);
                }
                tippy.LabelPanel.Visible = false;
                if (_currentMessage != null)
                    _secondsUntilNextState = _currentMessage.SlideTime;
                break;

            default: // finished hiding

                EntityManager.DeleteEntity(_entity);
                _entity = default;
                tippy.Visible = false;
                _currentMessage = null;
                _secondsUntilNextState = 0;
                tippy.State = TippyState.Hidden;
                break;
        }
    }

    private void OnScreenChanged((UIScreen? Old, UIScreen? New) ev)
    {
        ev.Old?.RemoveWidget<TippyUI>();
        _currentMessage = null;
        EntityManager.DeleteEntity(_entity);
    }
}
