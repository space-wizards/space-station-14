using System.Linq;
using Content.Client.Guidebook.Controls;
using Content.Client.Light;
using Content.Client.Verbs;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Light.Component;
using Content.Shared.Speech;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Guidebook;

/// <summary>
///     This system handles the help-verb and interactions with various client-side entities that are embedded into guidebooks.
/// </summary>
public sealed class GuidebookSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly VerbSystem _verbSystem = default!;
    [Dependency] private readonly RgbLightControllerSystem _rgbLightControllerSystem = default!;
    [Dependency] private readonly TagSystem _tags = default!;
    private GuidebookWindow _guideWindow = default!;

    public const string GuideEmbedTag = "GuideEmbeded";

    /// <inheritdoc/>
    public override void Initialize()
    {
        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenGuidebook,
                new PointerInputCmdHandler(HandleOpenGuidebook))
            .Register<GuidebookSystem>();
        _guideWindow = new GuidebookWindow();

        SubscribeLocalEvent<GuideHelpComponent, GetVerbsEvent<ExamineVerb>>(OnGetVerbs);
        SubscribeLocalEvent<GuidebookControlsTestComponent, InteractHandEvent>(OnGuidebookControlsTestInteractHand);
        SubscribeLocalEvent<GuidebookControlsTestComponent, ActivateInWorldEvent>(OnGuidebookControlsTestActivateInWorld);
        SubscribeLocalEvent<GuidebookControlsTestComponent, GetVerbsEvent<AlternativeVerb>>(
            OnGuidebookControlsTestGetAlternateVerbs);
    }

    private void OnGetVerbs(EntityUid uid, GuideHelpComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (component.Guides.Count == 0 || _tags.HasTag(uid, GuideEmbedTag))
            return;

        args.Verbs.Add(new()
        {
            Text = Loc.GetString("guide-help-verb"),
            IconTexture = "/Textures/Interface/VerbIcons/information.svg.192dpi.png",
            Act = () => OpenGuidebook(component.Guides, includeChildren: component.IncludeChildren, selected: component.Guides[0]),
            ClientExclusive = true,
            CloseMenu = true
        });
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
    /// <param name="includeChildren">Whether or not to automatically include child entries. If false, this will ONLY
    /// show the specified entries</param>
    /// <param name="selected">The guide whose contents should be displayed when the guidebook is opened</param>
    public bool OpenGuidebook(
        Dictionary<string, GuideEntry>? guides = null,
        List<string>? rootEntries = null,
        string? forceRoot = null,
        bool includeChildren = true,
        string? selected = null)
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
        else if (includeChildren)
        {
            var oldGuides = guides;
            guides = new(oldGuides);
            foreach (var guide in oldGuides.Values)
            {
                RecursivelyAddChildren(guide, guides);
            }
        }

        _guideWindow.UpdateGuides(guides, rootEntries, forceRoot, selected);

        return true;
    }

    public bool OpenGuidebook(
        List<string> guideList,
        List<string>? rootEntries = null,
        string? forceRoot = null,
        bool includeChildren = true,
        string? selected = null)
    {
        Dictionary<string, GuideEntry> guides = new();
        foreach (var guideId in guideList)
        {
            if (!_prototypeManager.TryIndex<GuideEntryPrototype>(guideId, out var guide))
            {
                Logger.Error($"Encountered unknown guide prototype: {guideId}");
                continue;
            }
            guides.Add(guideId, guide);
        }

        return OpenGuidebook(guides, rootEntries, forceRoot, includeChildren, selected);
    }

    private void RecursivelyAddChildren(GuideEntry guide, Dictionary<string, GuideEntry> guides)
    {
        foreach (var childId in guide.Children)
        {
            if (guides.ContainsKey(childId))
                continue;

            if (!_prototypeManager.TryIndex<GuideEntryPrototype>(childId, out var child))
            {
                Logger.Error($"Encountered unknown guide prototype: {childId} as a child of {guide.Id}. If the child is not a prototype, it must be directly provided.");
                continue;
            }

            guides.Add(childId, child);
            RecursivelyAddChildren(child, guides);
        }
    }
}

public sealed class GetGuidesEvent : EntityEventArgs
{
    public Dictionary<string, GuideEntry> Guides { get; init; } = new();
}
