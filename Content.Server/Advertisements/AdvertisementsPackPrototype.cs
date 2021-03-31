using System;
using System.Collections.Generic;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Advertisements
{
    [Serializable, Prototype("advertisementsPack")]
    public class AdvertisementsPackPrototype : IPrototype
    {
        [ViewVariables]
        [field: DataField("id", required: true)]
        public string ID { get; } = default!;

        [field: DataField("advertisements")]
        public List<string> Advertisements { get; } = new();
    }
}
