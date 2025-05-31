using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView : SpriteView
{
    private IClientPreferencesManager _preferencesManager = default!;
    private IPrototypeManager _prototypeManager = default!;
    private ISharedPlayerManager _playerManager = default!;
    private MetaDataSystem _metaDataSystem = default!;

    public string? ProfileName { get; private set; }
    public string? JobName { get; private set; }
    public string? LoadoutName { get; private set; }
    public string? FullDescription { get; private set; }

    public EntityUid PreviewDummy { get; private set; } = EntityUid.Invalid;

    public void Initialize(IClientPreferencesManager prefMan,
        IPrototypeManager protoMan,
        ISharedPlayerManager playerMan)
    {
        _preferencesManager = prefMan;
        _prototypeManager = protoMan;
        _playerManager = playerMan;
        _metaDataSystem = EntMan.System<MetaDataSystem>();
    }

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
                throw new NotImplementedException("Only humanoid profiles are implemented in ProfilePreviewSpriteView");
        }

        FullDescription = ConstructFullDescription();

        SetEntity(PreviewDummy);
        InvalidateMeasure();
        _metaDataSystem.SetEntityName(PreviewDummy, profile.Name);
    }

    public void ReloadProfilePreview(ICharacterProfile profile)
    {
        switch (profile)
        {
            case HumanoidCharacterProfile humanoid:
                ReloadHumanoidEntity(humanoid);
                break;
            default:
                throw new NotImplementedException("Only humanoid profiles are implemented in ProfilePreviewSpriteView");
        }
    }

    private string? ConstructFullDescription()
    {
        var description = ProfileName;
        if (LoadoutName != null)
            description = $"{description}\n\"{LoadoutName}\"";
        if (JobName != null)
            description = $"{description}\n{JobName}";
        return description;
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
