using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Lathe.Components;

/// <summary>
/// Causes this entity to announce onto the provided channels when it receives new recipes from its server
/// </summary>
[RegisterComponent]
public sealed partial class LatheAnnouncingComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<RadioChannelPrototype>> Channels = new();
}
