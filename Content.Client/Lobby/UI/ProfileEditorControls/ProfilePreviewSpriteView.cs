using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

/// <summary>
/// This class provides a control that gives you a sprite view of an entity with an applied
/// <see cref="HumanoidCharacterProfile"/>. It handles the loading and spawning of a profile and also extracts
/// information about the profile such as the profile's name, loadout override name, and preferred job if available.
/// </summary>
public sealed partial class ProfilePreviewSpriteView : SpriteView
{
    private IClientPreferencesManager _preferencesManager = default!;
    private IPrototypeManager _prototypeManager = default!;
    private ISharedPlayerManager _playerManager = default!;
    private MetaDataSystem _metaDataSystem = default!;

    /// <summary>
    /// The name of the loaded profile
    /// </summary>
    public string? ProfileName { get; private set; }

    /// <summary>
    /// The name of the preferred job of the loaded profile, if any
    /// </summary>
    public string? JobName { get; private set; }

    /// <summary>
    /// The job loadout override name of the loaded profile, if any
    /// </summary>
    public string? LoadoutName { get; private set; }

    /// <summary>
    /// The profile name, loadout override name, and preferred job name formatted into lines for use in
    /// something like the <see cref="CharacterPickerButton"/>.
    /// </summary>
    public string? FullDescription { get; private set; }

    /// <summary>
    /// The Uid of the currently loaded dummy
    /// </summary>
    public EntityUid PreviewDummy { get; private set; } = EntityUid.Invalid;

    /// <summary>
    /// This MUST be called before loading a profile to initialize the managers.
    /// Instead of resolving these dependencies, we pass the references through.
    /// </summary>
    /// <param name="prefMan">Passed in dependency</param>
    /// <param name="protoMan">Passed in dependency</param>
    /// <param name="playerMan">Passed in dependency</param>
    public void Initialize(IClientPreferencesManager prefMan,
        IPrototypeManager protoMan,
        ISharedPlayerManager playerMan)
    {
        _preferencesManager = prefMan;
        _prototypeManager = protoMan;
        _playerManager = playerMan;
        _metaDataSystem = EntMan.System<MetaDataSystem>();

        Stretch = StretchMode.None; //starlight
    }

    /// <summary>
    /// Create an entity from a character profile and display it in the sprite view.
    /// This can be used to reload the profile preview when job clothes change, but shouldn't be used for things on
    /// a slider like skin color. Use <see cref="ReloadProfilePreview"/> for that.
    /// </summary>
    /// <param name="profile">Character profile to load, currently only supports <see cref="HumanoidCharacterProfile"/></param>
    /// <param name="jobOverride">If null, attempt to find the character's preferred job, otherwise use this value</param>
    /// <param name="showClothes">If false, render the dummy without clothes</param>
    /// <exception cref="ArgumentException">Throws if something other than <see cref="HumanoidCharacterProfile"/> is passed in</exception>
    public void LoadPreview(ICharacterProfile profile, JobPrototype? jobOverride = null, bool showClothes = true)
    {
        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;

        switch (profile)
        {
            case HumanoidCharacterProfile humanoid:
                LoadHumanoidEntity(humanoid, jobOverride, showClothes);
                break;
            default:
                throw new ArgumentException("Only humanoid profiles are implemented in ProfilePreviewSpriteView");
        }

        FullDescription = ConstructFullDescription();

        SetEntity(PreviewDummy);
        InvalidateMeasure();
        _metaDataSystem.SetEntityName(PreviewDummy, profile.Name);
    }

    /// <summary>
    /// Reloads just the elements of <see cref="HumanoidAppearanceComponent"/>, this is useful to change skin color,
    /// markings, etc, as <see cref="LoadPreview"/> is much more expensive as it recreates the entire entity.
    /// </summary>
    /// <param name="profile"></param>
    /// <exception cref="ArgumentException"></exception>
    public void ReloadProfilePreview(ICharacterProfile profile)
    {
        switch (profile)
        {
            case HumanoidCharacterProfile humanoid:
                ReloadHumanoidEntity(humanoid);
                break;
            default:
                throw new ArgumentException("Only humanoid profiles are implemented in ProfilePreviewSpriteView");
        }
    }

    private string ConstructFullDescription()
    {
        var descriptionLines = new List<string>();

        if (ProfileName != null)
            descriptionLines.Add(ProfileName);

        if (LoadoutName != null)
            descriptionLines.Add($"\"{LoadoutName}\"");

        if (JobName != null)
            descriptionLines.Add(JobName);

        return string.Join("\n", descriptionLines);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;
    }
}
