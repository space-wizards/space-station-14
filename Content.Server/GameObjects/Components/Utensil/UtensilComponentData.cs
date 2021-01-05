using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Utensil;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Utensil
{
    public partial class UtensilComponentData
    {
        [CustomYamlField("types")] public UtensilType Types;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataReadWriteFunction("types",
                new List<UtensilType>(),
                types => types.ForEach((type) => Types |= type),
                () =>
                {
                    var types = new List<UtensilType>();

                    foreach (UtensilType type in Enum.GetValues(typeof(UtensilType)))
                    {
                        if ((Types & type) != 0)
                        {
                            types.Add(type);
                        }
                    }

                    return types;
                });
        }
    }
}
