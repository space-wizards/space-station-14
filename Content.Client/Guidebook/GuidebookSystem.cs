using System.Linq;
using Content.Client.Guidebook.Components;
using Content.Client.Light;
using Content.Client.Verbs;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Speech;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook;

/// <summary>
///     This system handles the help-verb and interactions with various client-side entities that are embedded into guidebooks.
/// </summary>
public sealed class GuidebookSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly VerbSystem _verbSystem = default!;
    [Dependency] private readonly RgbLightControllerSystem _rgbLightControllerSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public event Action<List<string>, List<string>?, string?, bool, string?>? OnGuidebookOpen;
    public const string GuideEmbedTag = "GuideEmbeded";

    private EntityUid _defaultUser;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GuideHelpComponent, GetVerbsEvent<ExamineVerb>>(OnGetVerbs);
        SubscribeLocalEvent<GuideHelpComponent, ActivateInWorldEvent>(OnInteract);

        SubscribeLocalEvent<GuidebookControlsTestComponent, InteractHandEvent>(OnGuidebookControlsTestInteractHand);
        SubscribeLocalEvent<GuidebookControlsTestComponent, ActivateInWorldEvent>(OnGuidebookControlsTestActivateInWorld);
        SubscribeLocalEvent<GuidebookControlsTestComponent, GetVerbsEvent<AlternativeVerb>>(
            OnGuidebookControlsTestGetAlternateVerbs);
    }

    /// <summary>
    /// Gets a user entity to use for verbs and examinations. If the player has no attached entity, this will use a
    /// dummy client-side entity so that users can still use the guidebook when not attached to anything (e.g., in the
    /// lobby)
    /// </summary>
    public EntityUid GetGuidebookUser()
    {
        var user = _playerManager.LocalEntity;
        if (user != null)
            return user.Value;

        if (!Exists(_defaultUser))
            _defaultUser = Spawn(null, MapCoordinates.Nullspace);

        return _defaultUser;
    }

    private void OnGetVerbs(EntityUid uid, GuideHelpComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (component.Guides.Count == 0 || _tags.HasTag(uid, GuideEmbedTag))
            return;

        args.Verbs.Add(new()
        {
            Text = Loc.GetString("guide-help-verb"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
            Act = () => OnGuidebookOpen?.Invoke(component.Guides, null, null, component.IncludeChildren, component.Guides[0]),
            ClientExclusive = true,
            CloseMenu = true
        });
    }

    private void OnInteract(EntityUid uid, GuideHelpComponent component, ActivateInWorldEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!component.OpenOnActivation || component.Guides.Count == 0 || _tags.HasTag(uid, GuideEmbedTag))
            return;

        OnGuidebookOpen?.Invoke(component.Guides, null, null, component.IncludeChildren, component.Guides[0]);
        args.Handled = true;
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
                EnsureComp<PointLightComponent>(uid); // RGB demands this.
                _pointLightSystem.SetEnabled(uid, false);
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
        var activateMsg = new ActivateInWorldEvent(GetGuidebookUser(), activated);
        RaiseLocalEvent(activated, activateMsg);
    }

    public void FakeClientAltActivateInWorld(EntityUid activated)
    {
        // Get list of alt-interact verbs
        var verbs = _verbSystem.GetLocalVerbs(activated, GetGuidebookUser(), typeof(AlternativeVerb), force: true);

        if (!verbs.Any())
            return;

        _verbSystem.ExecuteVerb(verbs.First(), GetGuidebookUser(), activated);
    }

    public void FakeClientUse(EntityUid activated)
    {
        var activateMsg = new InteractHandEvent(GetGuidebookUser(), activated);
        RaiseLocalEvent(activated, activateMsg);
    }
}
