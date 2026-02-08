using Robust.Shared.Prototypes;

namespace Content.Shared.Radio.EntitySystems;

public abstract class SharedRadioSystem : EntitySystem
{
    public virtual void SendRadioMessage(
        EntityUid messageSource,
        string message,
        ProtoId<RadioChannelPrototype> channel,
        EntityUid radioSource,
        bool escapeMarkup = true)
    { }
}
