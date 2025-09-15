// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NecromorfComponent : Component
{
    public NecromorfComponent(InfectionDeadStrainData sd)
    {
        StrainData = sd;
    }

    [DataField]
    public InfectionDeadStrainData StrainData = new InfectionDeadStrainData();

    [ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeedMultiply = 1f;

    /// <summary>
    /// The eye color of the Necromorf
    /// </summary>
    [DataField]
    public Color EyeColor = new(1f, 1f, 1f);

    /// <summary>
    /// The base layer to apply to any 'external' humanoid layers upon zombification.
    /// </summary>
    [DataField]
    public string BaseLayerExternal = "MobHumanoidMarkingMatchSkin";

    /// <summary>
    /// The attack arc of the Necromorf
    /// </summary>
    [DataField("attackArc")]
    public EntProtoId AttackAnimation = "WeaponArcBite";

    /// <summary>
    /// The EntityName of the humanoid to restore in case of cloning
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public string BeforeNecroficationEntityName = string.Empty;

    /// <summary>
    /// The CustomBaseLayers of the humanoid to restore in case of cloning
    /// </summary>
    [DataField]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> BeforeNecroficationCustomBaseLayers = new();

    /// <summary>
    /// The skin color of the humanoid to restore in case of cloning
    /// </summary>
    [DataField]
    public Color BeforeNecroficationSkinColor;

    /// <summary>
    /// The eye color of the humanoid to restore in case of cloning
    /// </summary>
    [DataField]
    public Color BeforeNecroficationEyeColor;

    [DataField("emoteId")]
    public ProtoId<EmoteSoundsPrototype> EmoteSoundsId = "Necro";
    public EmoteSoundsPrototype? EmoteSounds;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    /// <summary>
    /// Healing each second
    /// </summary>
    [DataField]
    public DamageSpecifier PassiveHealing = new()
    {
        DamageDict = new()
        {
            { "Blunt", -0.8 },
            { "Slash", -0.2 },
            { "Piercing", -1 },
            { "Heat", -0.05 },
            { "Shock", -0.1 }
        }
    };

    /// <summary>
    /// A multiplier applied to <see cref="PassiveHealing"/> when the entity is in critical condition.
    /// </summary>
    [DataField("passiveHealingCritMultiplier")]
    public float PassiveHealingCritMultiplier = 2f;

    /// <summary>
    ///     Hit sound on Necromorf bite.
    /// </summary>
    [DataField]
    public SoundSpecifier BiteSound = new SoundPathSpecifier("/Audio/Weapons/bladeslice.ogg");

    /// <summary>
    /// The blood reagent of the humanoid to restore in case of cloning
    /// </summary>
    [DataField]
    public string BeforeNecroficationBloodReagent = string.Empty;

    /// <summary>
    /// The blood reagent to give the Necromorf. In case you want Necromorfs that bleed milk, or something.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> NewBloodReagent = "NecromorfBlood";

    [DataField("useInventory")]
    public bool IsCanUseInventory = true;

    [DataField]
    public bool IsMutated = false;
}
