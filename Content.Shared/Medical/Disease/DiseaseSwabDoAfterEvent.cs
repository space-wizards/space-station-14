using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Disease;

[Serializable, NetSerializable]
public sealed partial class DiseaseSwabDoAfterEvent : SimpleDoAfterEvent
{
}
