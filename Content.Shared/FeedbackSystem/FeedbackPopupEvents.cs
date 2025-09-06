using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.FeedbackSystem;

/// <summary>
/// When clients receive this message a popup will appear with the contents from the given prototypes.
/// </summary>
[Serializable, NetSerializable]
public sealed class FeedbackPopupMessage(List<ProtoId<FeedbackPopupPrototype>> feedbackPrototypes) : EntityEventArgs
{
    public List<ProtoId<FeedbackPopupPrototype>> FeedbackPrototypes = feedbackPrototypes;
}
