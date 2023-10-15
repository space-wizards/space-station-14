// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using System.Collections.Generic;

namespace Robust.Shared.Audio;

[Prototype("touretteCollection")]
public sealed class TouretteCollectionPrototype : IPrototype
{
    [ViewVariables, IdDataField] public string ID { get; private set; } = default!;
    [DataField("replics")]
    public List<string> Replics { get; private set; } = new();
}
