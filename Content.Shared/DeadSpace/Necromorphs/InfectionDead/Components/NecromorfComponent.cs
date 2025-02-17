// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Chat.Prototypes;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class NecromorfComponent : Component
{
    public NecromorfComponent()
    { }

    [ViewVariables(VVAccess.ReadWrite)]
    public float MovementSpeedMultiply = 1f;

    [DataField("skinColor")]
    public Color SkinColor = new(0.64f, 0.60f, 0.72f);

    /// <summary>
    /// The eye color of the Necromorf
    /// </summary>
    [DataField("eyeColor")]
    public Color EyeColor = new(1f, 0f, 0f);

    /// <summary>
    /// The base layer to apply to any 'external' humanoid layers upon zombification.
    /// </summary>
    [DataField("baseLayerExternal")]
    public string BaseLayerExternal = "MobHumanoidMarkingMatchSkin";

    /// <summary>
    /// The attack arc of the Necromorf
    /// </summary>
    [DataField("attackArc", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string AttackAnimation = "WeaponArcBite";

    /// <summary>
    /// The EntityName of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeNecroficationEntityName"), ViewVariables(VVAccess.ReadOnly)]
    public string BeforeNecroficationEntityName = string.Empty;

    /// <summary>
    /// The CustomBaseLayers of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeNecroficationCustomBaseLayers")]
    public Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> BeforeNecroficationCustomBaseLayers = new();

    /// <summary>
    /// The skin color of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeNecroficationSkinColor")]
    public Color BeforeNecroficationSkinColor;

    /// <summary>
    /// The eye color of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeNecroficationEyeColor")]
    public Color BeforeNecroficationEyeColor;

    [DataField("emoteId", customTypeSerializer: typeof(PrototypeIdSerializer<EmoteSoundsPrototype>))]
    public string? EmoteSoundsId = "Necro";
    public EmoteSoundsPrototype? EmoteSounds;

    [DataField("nextTick", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    /// <summary>
    /// Healing each second
    /// </summary>
    [DataField("passiveHealing")]
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
    public float PassiveHealingCritMultiplier = 4f;

    /// <summary>
    ///     Hit sound on Necromorf bite.
    /// </summary>
    [DataField]
    public SoundSpecifier BiteSound = new SoundPathSpecifier("/Audio/Weapons/bladeslice.ogg");

    /// <summary>
    /// The blood reagent of the humanoid to restore in case of cloning
    /// </summary>
    [DataField("beforeNecroficationBloodReagent")]
    public string BeforeNecroficationBloodReagent = string.Empty;

    /// <summary>
    /// The blood reagent to give the Necromorf. In case you want Necromorfs that bleed milk, or something.
    /// </summary>
    [DataField("newBloodReagent", customTypeSerializer: typeof(PrototypeIdSerializer<ReagentPrototype>))]
    public string NewBloodReagent = "NecromorfBlood";

    [DataField("useInventory")]
    public bool IsCanUseInventory = true;
}
