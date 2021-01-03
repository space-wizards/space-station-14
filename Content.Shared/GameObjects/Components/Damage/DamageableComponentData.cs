using System;
using System.Collections.Generic;
using Content.Shared.Damage;
using Content.Shared.Damage.DamageContainer;
using Content.Shared.Damage.ResistanceSet;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Damage
{
    public class DamageableComponentData : Component_AUTODATA
    {
        // TODO define these in yaml?
        public const string DefaultResistanceSet = "defaultResistances";
        public const string DefaultDamageContainer = "metallicDamageContainer";

        [CustomYamlField("flags")] public DamageFlag Flags;
        [CustomYamlField("resistances")] public ResistanceSet Resistances { get; set; }
        [CustomYamlField("damageContainer")] public string DamageContainerId { get; set; }
        [CustomYamlField("supportedTypes")]
        public readonly HashSet<DamageType> _supportedTypes = new();
        [CustomYamlField("supportedClasses")]
        public readonly HashSet<DamageClass> _supportedClasses = new();

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            serializer.DataReadWriteFunction(
                "flags",
                new List<DamageFlag>(),
                flags =>
                {
                    var result = DamageFlag.None;

                    foreach (var flag in flags)
                    {
                        result |= flag;
                    }

                    Flags = result;
                },
                () =>
                {
                    var writeFlags = new List<DamageFlag>();

                    if (Flags == DamageFlag.None)
                    {
                        return writeFlags;
                    }

                    foreach (var flag in (DamageFlag[]) Enum.GetValues(typeof(DamageFlag)))
                    {
                        if ((Flags & flag) == flag)
                        {
                            writeFlags.Add(flag);
                        }
                    }

                    return writeFlags;
                });

            // TODO DAMAGE Serialize damage done and resistance changes
            serializer.DataReadWriteFunction(
                "damageContainer",
                DefaultDamageContainer,
                prototype =>
                {
                    var damagePrototype = prototypeManager.Index<DamageContainerPrototype>(prototype);

                    _supportedClasses.Clear();
                    _supportedTypes.Clear();

                    DamageContainerId = damagePrototype.ID;
                    _supportedClasses.UnionWith(damagePrototype.SupportedClasses);
                    _supportedTypes.UnionWith(damagePrototype.SupportedTypes);
                },
                () => DamageContainerId);

            serializer.DataReadWriteFunction(
                "resistances",
                DefaultResistanceSet,
                prototype =>
                {
                    var resistancePrototype = prototypeManager.Index<ResistanceSetPrototype>(prototype);
                    Resistances = new ResistanceSet(resistancePrototype);
                },
                () => Resistances.ID);
        }
    }
}
