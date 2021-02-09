using System;
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Utensil;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Nutrition
{
    public partial class FoodComponentData
    {
        [DataClassTarget("utensilsNeeded")] public UtensilType? UtensilsNeeded;

        public void ExposeData(ObjectSerializer serializer)
        {
            UtensilsNeeded ??= UtensilType.None;
            serializer.DataReadWriteFunction(
                "utensils",
                new List<UtensilType>(),
                types => types.ForEach(type => UtensilsNeeded |= type),
                () =>
                {
                    var types = new List<UtensilType>();

                    foreach (var type in (UtensilType[]) Enum.GetValues(typeof(UtensilType)))
                    {
                        if ((UtensilsNeeded & type) != 0)
                        {
                            types.Add(type);
                        }
                    }

                    return types;
                });
            if (UtensilsNeeded == UtensilType.None) UtensilsNeeded = null;
        }
    }
}
