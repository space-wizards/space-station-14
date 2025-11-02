using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView : SpriteView
{
    private IPrototypeManager _prototypeManager = default!;
    private ISharedPlayerManager _playerManager = default!;

    /// <summary>
    /// Entity used for the profile editor preview
    /// </summary>
    public EntityUid PreviewDummy;

    /// <summary>
    /// This MUST be called before loading a profile to initialize the managers.
    /// Instead of resolving these dependencies, we pass the references through.
    /// </summary>
    /// <param name="protoMan">Passed in dependency</param>
    /// <param name="playerMan">Passed in dependency</param>
    public void Initialize(IPrototypeManager protoMan,
        ISharedPlayerManager playerMan)
    {
        _prototypeManager = protoMan;
        _playerManager = playerMan;
    }

    /// <summary>
    /// Reloads the entire dummy entity for preview.
    /// </summary>
    /// <remarks>
    /// This is expensive so not recommended to run if you have a slider.
    /// </remarks>
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
