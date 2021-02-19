#nullable enable
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Damage
{
    public partial class DamageableComponentData : ISerializationHooks
    {
        // TODO define these in yaml?
        public const string DefaultResistanceSet = "defaultResistances";
        public const string DefaultDamageContainer = "metallicDamageContainer";

        [DataField("resistances")] public string? ResistanceSetId;

        [DataClassTarget("resistancesTarget")]
        public ResistanceSet? Resistances { get; set; }

        [DataField("damageContainer")] [DataClassTarget("damageContainer")]
        public string? DamageContainerId { get; set; }

        [DataClassTarget("supportedTypes")]
        private readonly HashSet<DamageType> _supportedTypes = new();

        [DataClassTarget("supportedClasses")]
        private readonly HashSet<DamageClass> _supportedClasses = new();

        public void AfterDeserialization()
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            // TODO DAMAGE Serialize damage done and resistance changes
            if (DamageContainerId != null)
            {
                var damagePrototype = prototypeManager.Index<DamageContainerPrototype>(DamageContainerId);

                _supportedClasses.Clear();
                _supportedTypes.Clear();

                DamageContainerId = damagePrototype.ID;
                _supportedClasses.UnionWith(damagePrototype.SupportedClasses);
                _supportedTypes.UnionWith(damagePrototype.SupportedTypes);
            }

            if (ResistanceSetId != null)
            {
                var resistancePrototype = prototypeManager.Index<ResistanceSetPrototype>(ResistanceSetId);
                Resistances = new ResistanceSet(resistancePrototype);
            }
        }
    }
}
