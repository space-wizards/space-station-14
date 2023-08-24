using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Server.Zombies;

[RegisterComponent, Access(typeof(ZombieSystem))]
public sealed partial class BeforeZombieComponent : Component
{
    /// <summary>
    /// The EntityName of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedEntityName"), ViewVariables(VVAccess.ReadOnly)]
    public string BeforeZombifiedEntityName = string.Empty;

    /// <summary>
    /// The CustomBaseLayers of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedCustomBaseLayers")]
    public Dictionary<HumanoidVisualLayers, HumanoidAppearanceState.CustomBaseLayerInfo> BeforeZombifiedCustomBaseLayers = new ();

    /// <summary>
    /// The skin color of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedSkinColor")]
    public Color BeforeZombifiedSkinColor;

    /// <summary>
    /// The blood reagent of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedBloodReagent")]
    public string BeforeZombifiedBloodReagent = string.Empty;

    /// <summary>
    /// The eye color of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeZombifiedEyeColor")]
    public Color BeforeZombifiedEyeColor;
}
