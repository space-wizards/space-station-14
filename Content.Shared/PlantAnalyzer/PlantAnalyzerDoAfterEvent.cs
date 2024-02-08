using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using System.Threading;

namespace Content.Shared.PlantAnalyzer;

[Serializable, NetSerializable]
public sealed partial class PlantAnalyzerDoAfterEvent : SimpleDoAfterEvent
{
    public CancellationTokenWrapper CancellationTokenWrapper { get; set; }
}
