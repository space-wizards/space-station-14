using System.Linq;
using Content.Client.Guidebook.Controls;
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

// TODO GUIDEBOOKS
// - improve Tree UI control to add highlighting & collapsible sections
// - search bar for sections/guides
// - add help component/verb
//   - Examine tooltip -> ? button -> opens a relevant guide
//   - Maybe also a "help" keybind that tries to open a relevant guide based on the mouse's current control/window or hovered entity.
// - Tests. Especially for all the parsing stuff.
// - Hide tree view when showing a singular guide.

/// <summary>
///     This system handles interactions with various client-side entities that are embedded into guidebooks.
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
            Text = Loc.GetString("guidebook-monkey-unspin"),
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
            Text = Loc.GetString("guidebook-monkey-disco"),
            Priority = -9998,
        });
    }

    private void OnGuidebookControlsTestActivateInWorld(EntityUid uid, GuidebookControlsTestComponent component, ActivateInWorldEvent args)
    {
        Transform(uid).LocalRotation += Angle.FromDegrees(90);
    }

    private void OnGuidebookControlsTestInteractHand(EntityUid uid, GuidebookControlsTestComponent component, InteractHandEvent args)
    {
        if (!TryComp<SpeechComponent>(uid, out var speech) || speech.SpeechSounds is null)
            return;

        _audioSystem.PlayGlobal(speech.SpeechSounds, Filter.Local(), false, speech.AudioParams);
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

    private bool HandleOpenGuidebook(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        if (args.State != BoundKeyState.Down)
            return false;

        OpenGuidebook();
        return true;
    }

    /// <summary>
    ///     Opens the guidebook.
    /// </summary>
    /// <param name="guides">What guides should be shown. If not specified, this will instead raise a <see
    /// cref="GetGuidesEvent"/> and automatically include all guide prototypes.</param>
    /// <param name="rootEntries">A list of guides that should form the base of the table of contents. If not specified,
    /// this will automatically simply be a list of all guides that have no parent.</param>
    /// <param name="forceRoot">This forces a singular guide to contain all other guides. This guide will
    /// contain its own children, in addition to what would normally be the root guides if this were not
    /// specified.</param>
    /// <returns></returns>
    public bool OpenGuidebook(Dictionary<string, GuideEntry>? guides = null, List<string>? rootEntries = null, string? forceRoot = null)
    {
        _guideWindow.OpenCenteredRight();

        if (guides == null)
        {
            var ev = new GetGuidesEvent()
            {
                Guides = _prototypeManager.EnumeratePrototypes<GuideEntryPrototype>().ToDictionary(x => x.ID, x => (GuideEntry) x)
            };
            RaiseLocalEvent(ev);
            guides = ev.Guides;
        }

        _guideWindow.UpdateGuides(guides, rootEntries, forceRoot);

        return true;
    }
}

public sealed class GetGuidesEvent : EntityEventArgs
{
    public Dictionary<string, GuideEntry> Guides { get; init; } = new();
}
