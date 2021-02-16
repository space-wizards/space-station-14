using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Utensil;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Utensil
{
    public partial class UtensilComponentData
    {
        [DataClassTarget("types")] public UtensilType Types;

        public void ExposeData(ObjectSerializer serializer)
        {
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
