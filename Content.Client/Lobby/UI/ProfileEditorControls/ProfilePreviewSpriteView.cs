using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView : SpriteView
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;

    /// <summary>
    /// Entity used for the profile editor preview
    /// </summary>
    public EntityUid PreviewDummy;

    public ProfilePreviewSpriteView()
    {
        IoCManager.InjectDependencies(this);
    }

    /// <summary>
    /// Reloads the entire dummy entity for preview.
    /// </summary>
    /// <remarks>
    /// This is expensive so not recommended to run if you have a slider.
    /// </remarks>
    public void LoadPreview(HumanoidCharacterProfile profile, JobPrototype? jobOverride = null, bool showClothes = true)
    {
        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;

        LoadHumanoidEntity(profile, jobOverride, showClothes);

        SetEntity(PreviewDummy);
        SetName(profile.Name);
    }

    /// <summary>
    /// Sets the preview entity's name without reloading anything else.
    /// </summary>
    public void SetName(string newName)
    {
        EntMan.System<MetaDataSystem>().SetEntityName(PreviewDummy, newName);
    }

    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    public void ReloadProfilePreview(HumanoidCharacterProfile profile)
    {
        ReloadHumanoidEntity(profile);
    }

    public void ClearPreview()
    {
        EntMan.DeleteEntity(PreviewDummy);
        PreviewDummy = EntityUid.Invalid;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();
        ClearPreview();
    }
}
