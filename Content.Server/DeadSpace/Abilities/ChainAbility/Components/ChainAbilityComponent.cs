using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.DeadSpace.Abilities.ChainAbility.Components;

[RegisterComponent]
public sealed partial class ChainAbilityComponent : Component
{
    [DataField]
    public EntProtoId ChainAbility = "ActionChainAbility";

    [DataField, AutoNetworkedField]
    public EntityUid? ChainAbilityActionEntity;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string HandcuffsProtorype = "Handcuffs";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string MaskMuzzleProtorype = "ClothingSpiderMaskMuzzle";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string BandageProtorype = "ClothingEyesBlindfold";

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string BootsProtorype = "ClothingShoesBootsLaceupWeb";

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan ChainDuration = TimeSpan.FromSeconds(3);

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool NeedMuzzle = true;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool NeedBandage = true;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool NeedBoots = true;

}
