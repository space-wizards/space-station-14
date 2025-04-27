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
    private IEntityManager _entManager = default!;
    private ISharedPlayerManager _playerManager = default!;

    public string? ProfileName { get; private set; }
    public string? JobName { get; private set; }
    public string? LoadoutName { get; private set; }
    public string? FullDescription { get; private set; }

    public EntityUid PreviewDummy { get; private set; } = EntityUid.Invalid;

    public void Initialize(IClientPreferencesManager prefMan,
        IPrototypeManager protoMan,
        IEntityManager entMan,
        ISharedPlayerManager playerMan)
    {
        _preferencesManager = prefMan;
        _prototypeManager = protoMan;
        _entManager = entMan;
        _playerManager = playerMan;
    }

    public void LoadPreview(ICharacterProfile profile, JobPrototype? jobOverride = null, bool showClothes = true)
    {
        _entManager.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;

        switch (profile)
        {
            case HumanoidCharacterProfile humanoid:
                LoadHumanoidEntity(humanoid, jobOverride, showClothes);
                break;
            default:
                return;
        }

        FullDescription = ConstructFullDescription();

        SetEntity(PreviewDummy);
        InvalidateMeasure();
        _entManager.System<MetaDataSystem>().SetEntityName(PreviewDummy, profile.Name);
    }

    public void ReloadProfilePreview(ICharacterProfile profile)
    {
        switch (profile)
        {
            case HumanoidCharacterProfile humanoid:
                ReloadHumanoidEntity(humanoid);
                break;
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

        _entManager.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;
    }
}
