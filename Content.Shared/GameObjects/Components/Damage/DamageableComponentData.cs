#nullable enable
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
    public partial class DamageableComponentData
    {
        // TODO define these in yaml?
        public const string DefaultResistanceSet = "defaultResistances";
        public const string DefaultDamageContainer = "metallicDamageContainer";

        [CustomYamlField("flags")] public DamageFlag? Flags;
        [CustomYamlField("resistances")] public ResistanceSet? Resistances { get; set; }
        [CustomYamlField("damageContainer")] public string? DamageContainerId { get; set; }
        [CustomYamlField("supportedTypes")]
        public HashSet<DamageType>? SupportedTypes;
        [CustomYamlField("supportedClasses")]
        public HashSet<DamageClass>? SupportedClasses;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();

            Flags ??= DamageFlag.None;
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
            if (Flags == DamageFlag.None)
            {
                Flags = null;
            }

            // TODO DAMAGE Serialize damage done and resistance changes
            SupportedClasses ??= new();
            SupportedTypes ??= new();
            serializer.DataReadWriteFunction(
                "damageContainer",
                DefaultDamageContainer,
                prototype =>
                {
                    if(prototype == null) return;
                    var damagePrototype = prototypeManager.Index<DamageContainerPrototype>(prototype);

                    SupportedClasses.Clear();
                    SupportedTypes.Clear();

                    DamageContainerId = damagePrototype.ID;
                    SupportedClasses.UnionWith(damagePrototype.SupportedClasses);
                    SupportedTypes.UnionWith(damagePrototype.SupportedTypes);
                },
                () => DamageContainerId);
            if (SupportedClasses.Count == 0) SupportedClasses = null;
            if (SupportedTypes.Count == 0) SupportedTypes = null;

            serializer.DataReadWriteFunction(
                "resistances",
                DefaultResistanceSet,
                prototype =>
                {
                    if(prototype == null) return;
                    var resistancePrototype = prototypeManager.Index<ResistanceSetPrototype>(prototype);
                    Resistances = new ResistanceSet(resistancePrototype);
                },
                () => Resistances?.ID);
        }
    }
}
