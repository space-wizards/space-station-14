using System.Linq;
using Content.Client.Light;
using Content.Client.Verbs;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Light.Component;
using Content.Shared.Speech;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Guidebook;

/// <summary>
/// This handles...
/// </summary>
public sealed class GuidebookSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly VerbSystem _verbSystem = default!;
    [Dependency] private readonly RgbLightControllerSystem _rgbLightControllerSystem = default!;
    private GuidebookWindow _guideWindow = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenGuidebook,
                new PointerInputCmdHandler(HandleOpenGuidebook))
            .Register<GuidebookSystem>();
        _guideWindow = new GuidebookWindow();

        SubscribeLocalEvent<GetGuidesEvent>(OnGetGuidesEvent);
        SubscribeLocalEvent<GuidebookControlsTestComponent, InteractHandEvent>(OnGuidebookControlsTestInteractHand);
        SubscribeLocalEvent<GuidebookControlsTestComponent, ActivateInWorldEvent>(OnGuidebookControlsTestActivateInWorld);
        SubscribeLocalEvent<GuidebookControlsTestComponent, GetVerbsEvent<AlternativeVerb>>(
            OnGuidebookControlsTestGetAlternateVerbs);
    }


    private void OnGuidebookControlsTestGetAlternateVerbs(EntityUid uid, GuidebookControlsTestComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                if (Transform(uid).LocalRotation != Angle.Zero)
                    Transform(uid).LocalRotation -= Angle.FromDegrees(90);
            },
            Text = "Unspin Monkey",
            Priority = -9999,
        });

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                var light = EnsureComp<PointLightComponent>(uid); // RGB demands this.
                light.Enabled = false;
                var rgb = EnsureComp<RgbLightControllerComponent>(uid);

                var sprite = EnsureComp<SpriteComponent>(uid);
                var layers = new List<int>();

                for (var i = 0; i < sprite.AllLayers.Count(); i++)
                {
                    layers.Add(i);
                }

                _rgbLightControllerSystem.SetLayers(uid, layers, rgb);
            },
            Text = "Disco Monkey",
            Priority = -9998,
        });
    }

    private void OnGuidebookControlsTestActivateInWorld(EntityUid uid, GuidebookControlsTestComponent component, ActivateInWorldEvent args)
    {
        Transform(uid).LocalRotation += Angle.FromDegrees(90);
    }

    private void OnGuidebookControlsTestInteractHand(EntityUid uid, GuidebookControlsTestComponent component, InteractHandEvent args)
    {
        if (!TryComp<SharedSpeechComponent>(uid, out var speech) || speech.SpeechSounds is null)
            return;

        _audioSystem.PlayGlobal(speech.SpeechSounds, Filter.Local(), speech.AudioParams);
    }


    public void FakeClientActivateInWorld(EntityUid activated)
    {
        var user = _playerManager.LocalPlayer!.ControlledEntity;
        if (user is null)
            return;
        var activateMsg = new ActivateInWorldEvent(user.Value, activated);
        RaiseLocalEvent(activated, activateMsg, true);
    }

    public void FakeClientAltActivateInWorld(EntityUid activated)
    {
        var user = _playerManager.LocalPlayer!.ControlledEntity;
        if (user is null)
            return;
        // Get list of alt-interact verbs
        var verbs = _verbSystem.GetLocalVerbs(activated, user.Value, typeof(AlternativeVerb));

        if (!verbs.Any())
            return;

        _verbSystem.ExecuteVerb(verbs.First(), user.Value, activated);
    }

    public void FakeClientUse(EntityUid activated)
    {
        var user = _playerManager.LocalPlayer!.ControlledEntity ?? EntityUid.Invalid;
        var activateMsg = new InteractHandEvent(user, activated);
        RaiseLocalEvent(activated, activateMsg, true);
    }


    private void OnGetGuidesEvent(GetGuidesEvent ev)
    {
        ev.Entries.AddRange(_prototypeManager.EnumeratePrototypes<GuideEntryPrototype>());
    }

    private bool HandleOpenGuidebook(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State == BoundKeyState.Down)
            _guideWindow.OpenCenteredRight();

        var ev = new GetGuidesEvent();
        RaiseLocalEvent(ev);

        _guideWindow.UpdateGuides(ev.Entries);

        return true;
    }
}

public sealed class GetGuidesEvent : EntityEventArgs
{
    public readonly List<GuideEntry> Entries = new();
}
