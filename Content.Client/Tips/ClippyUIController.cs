using Content.Client.Gameplay;
using System.Numerics;
using Content.Client.Message;
using Content.Client.Paper;
using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Content.Shared.Tips;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using static Content.Client.Tips.ClippyUI;

namespace Content.Client.Tips;

public sealed class ClippyUIController : UIController
{
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;
    [UISystemDependency] private readonly AudioSystem _audio = default!;

    public const float Padding = 50;
    public static Angle WaddleRotation = Angle.FromDegrees(10);

    private EntityUid _entity;
    private float _secondsUntilNextState;
    private int _previousStep = 0;
    private ClippyEvent? _currentMessage;
    private readonly Queue<ClippyEvent> _queuedMessages = new();

    public override void Initialize()
    {
        base.Initialize();

        _conHost.RegisterCommand("local_clippy", ClippyCommand);
        UIManager.OnScreenChanged += OnScreenChanged;
    }

    private void ClippyCommand(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length == 0)
        {
            shell.WriteLine("usage: clippy <message> [entity prototype] [speak time] [animate time] [waddle]");
            return;
        }

        var ev = new ClippyEvent(args[0]);

        string proto;
        if (args.Length > 1)
        {
            ev.Proto = args[1];

            if (!_protoMan.HasIndex<EntityPrototype>(ev.Proto))
            {
                shell.WriteError($"Unknown prototype: {ev.Proto}");
                return;
            }
        }


        if (args.Length > 2)
            ev.SpeakTime = float.Parse(args[2]);

        if (args.Length > 3)
            ev.SlideTime = float.Parse(args[3]);

        if (args.Length > 4)
            ev.WaddleInterval = float.Parse(args[4]);

        AddMessage(ev);
    }

    public void AddMessage(ClippyEvent ev)
    {
        _queuedMessages.Enqueue(ev);
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

        var clippy = screen.GetOrAddWidget<ClippyUI>();
        _secondsUntilNextState -= args.DeltaSeconds;

        if (_secondsUntilNextState <= 0)
            NextState(clippy);
        else
        {
            var pos = UpdatePosition(clippy, screen.Size, args); ;
            LayoutContainer.SetPosition(clippy, pos);
        }
    }

    private Vector2 UpdatePosition(ClippyUI clippy, Vector2 screenSize, FrameEventArgs args)
    {
        if (_currentMessage == null)
            return default;

        var slideTime = _currentMessage.SlideTime;

        var offset = clippy.State switch
        {
            ClippyState.Hidden => 0,
            ClippyState.Revealing => Math.Clamp(1 - _secondsUntilNextState / slideTime, 0, 1),
            ClippyState.Hiding => Math.Clamp(_secondsUntilNextState / slideTime, 0, 1),
            _ => 1,
        };

        var waddle = _currentMessage.WaddleInterval;

        if (_currentMessage == null
            || waddle <= 0
            || clippy.State == ClippyState.Hidden
            || clippy.State == ClippyState.Speaking
            || !EntityManager.TryGetComponent(_entity, out SpriteComponent? sprite))
        {
            return new Vector2(screenSize.X - offset * (clippy.DesiredSize.X + Padding), (screenSize.Y - clippy.DesiredSize.Y) / 2);
        }

        var numSteps = (int) Math.Ceiling(slideTime / waddle);
        var curStep = (int) Math.Floor(numSteps * offset);
        var stepSize = (clippy.DesiredSize.X + Padding) / numSteps;

        if (curStep != _previousStep)
        {
            _previousStep = curStep;
            sprite.Rotation = sprite.Rotation > 0
                ? -WaddleRotation
                : WaddleRotation;

            if (EntityManager.TryGetComponent(_entity, out FootstepModifierComponent? step))
            {
                var audioParams = step.FootstepSoundCollection.Params
                    .AddVolume(-7f)
                    .WithVariation(0.1f);
                _audio.PlayGlobal(step.FootstepSoundCollection, EntityUid.Invalid, audioParams);
            }
        }

        return new Vector2(screenSize.X - stepSize * curStep, (screenSize.Y - clippy.DesiredSize.Y) / 2);
    }

    private void NextState(ClippyUI clippy)
    {
        SpriteComponent? sprite;
        switch (clippy.State)
        {
            case ClippyState.Hidden:
                if (!_queuedMessages.TryDequeue(out var next))
                    return;

                if (next.Proto != null)
                {
                    _entity = EntityManager.SpawnEntity(next.Proto, MapCoordinates.Nullspace);
                    clippy.ModifyLayers = false;
                }
                else
                {
                    _entity = EntityManager.SpawnEntity(_cfg.GetCVar(CCVars.ClippyEntity), MapCoordinates.Nullspace);
                    clippy.ModifyLayers = true;
                }
                if (!EntityManager.TryGetComponent(_entity, out sprite))
                    return;
                clippy.InitLabel(EntityManager.GetComponentOrNull<PaperVisualsComponent>(_entity), _resCache);

                var scale = sprite.Scale;
                if (clippy.ModifyLayers)
                {
                    sprite.Scale = Vector2.One;
                }
                else
                {
                    sprite.Scale = new Vector2(2, 2);
                }
                clippy.Entity.SetEntity(_entity);
                clippy.Entity.Scale = scale;

                _currentMessage = next;
                _secondsUntilNextState = next.SlideTime;
                clippy.State = ClippyState.Revealing;
                _previousStep = 0;
                if (clippy.ModifyLayers)
                {
                    sprite.LayerSetAnimationTime("revealing", 0);
                    sprite.LayerSetVisible("revealing", true);
                    sprite.LayerSetVisible("speaking", false);
                    sprite.LayerSetVisible("hiding", false);
                }
                sprite.Rotation = 0;
                clippy.Label.SetMarkup(_currentMessage.Msg);
                clippy.LabelPanel.Visible = false;
                clippy.Visible = true;
                sprite.Visible = true;
                break;

            case ClippyState.Revealing:
                clippy.State = ClippyState.Speaking;
                if (!EntityManager.TryGetComponent(_entity, out sprite))
                    return;
                sprite.Rotation = 0;
                _previousStep = 0;
                if (clippy.ModifyLayers)
                {
                    sprite.LayerSetAnimationTime("speaking", 0);
                    sprite.LayerSetVisible("revealing", false);
                    sprite.LayerSetVisible("speaking", true);
                    sprite.LayerSetVisible("hiding", false);
                }
                clippy.LabelPanel.Visible = true;
                clippy.InvalidateArrange();
                clippy.InvalidateMeasure();
                if (_currentMessage != null)
                    _secondsUntilNextState = _currentMessage.SpeakTime;

                break;

            case ClippyState.Speaking:
                clippy.State = ClippyState.Hiding;
                if (!EntityManager.TryGetComponent(_entity, out sprite))
                    return;
                if (clippy.ModifyLayers)
                {
                    sprite.LayerSetAnimationTime("hiding", 0);
                    sprite.LayerSetVisible("revealing", false);
                    sprite.LayerSetVisible("speaking", false);
                    sprite.LayerSetVisible("hiding", true);
                }
                clippy.LabelPanel.Visible = false;
                if (_currentMessage != null)
                    _secondsUntilNextState = _currentMessage.SlideTime;
                break;

            default: // finished hiding

                EntityManager.DeleteEntity(_entity);
                _entity = default;
                clippy.Visible = false;
                _currentMessage = null;
                _secondsUntilNextState = 0;
                clippy.State = ClippyState.Hidden;
                break;
        }
    }

    private void OnScreenChanged((UIScreen? Old, UIScreen? New) ev)
    {
        ev.Old?.RemoveWidget<ClippyUI>();
        _currentMessage = null;
        EntityManager.DeleteEntity(_entity);
    }
}
