using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Disease;

/// <summary>
/// Event for the <see cref="DiseaseSwabSystem"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DiseaseSwabDoAfterEvent : SimpleDoAfterEvent;
