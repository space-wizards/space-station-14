using Content.Shared.Cloning;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Materials;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Cloning.Components
{
    [RegisterComponent]
    public sealed class CloningPodComponent : Component
    {
        public const string PodPort = "CloningPodReceiver";

        [ViewVariables]
        public ContainerSlot BodyContainer = default!;

        /// <summary>
        /// How long the cloning has been going on for.
        /// </summary>
        [ViewVariables]
        public float CloningProgress = 0;

        [ViewVariables]
        public int UsedBiomass = 70;

        [ViewVariables]
        public bool FailedClone = false;

        /// <summary>
        /// The material that is used to clone entities.
        /// </summary>
        [DataField("requiredMaterial", customTypeSerializer: typeof(PrototypeIdSerializer<MaterialPrototype>)), ViewVariables(VVAccess.ReadWrite)]
        public string RequiredMaterial = "Biomass";

        /// <summary>
        /// The entity that is spawned on machine deconstruct as well as failed cloning.
        /// </summary>
        [DataField("materialCloningOutput", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), ViewVariables(VVAccess.ReadWrite)]
        public string MaterialCloningOutput = "MaterialBiomass";

        /// <summary>
        /// The base amount of time it takes to clone a body
        /// </summary>
        [DataField("baseCloningTime")]
        public float BaseCloningTime = 30f;

        /// <summary>
        /// The multiplier for cloning duration
        /// </summary>
        [DataField("partRatingSpeedMultiplier")]
        public float PartRatingSpeedMultiplier = 0.75f;

        /// <summary>
        /// The machine part that affects cloning speed
        /// </summary>
        [DataField("machinePartCloningSpeed", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartCloningSpeed = "ScanningModule";

        /// <summary>
        /// The current amount of time it takes to clone a body
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float CloningTime = 30f;

        /// <summary>
        /// The machine part that affects how much biomass is needed to clone a body.
        /// </summary>
        [DataField("partRatingMaterialMultiplier")]
        public float PartRatingMaterialMultiplier = 0.85f;

        /// <summary>
        /// The current multiplier on the body weight, which determines the
        /// amount of biomass needed to clone.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public float BiomassRequirementMultiplier = 1;

        /// <summary>
        /// The machine part that decreases the amount of material needed for cloning
        /// </summary>
        [DataField("machinePartMaterialUse", customTypeSerializer: typeof(PrototypeIdSerializer<MachinePartPrototype>))]
        public string MachinePartMaterialUse = "Manipulator";

        [ViewVariables(VVAccess.ReadWrite)]
        public CloningPodStatus Status;

        [ViewVariables]
        public EntityUid? ConnectedConsole;
    }
}
