// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Stamp;

[RegisterComponent]
public sealed partial class LawyerStampProviderComponent : Component
{
    [DataField]
    public EntProtoId StampPrototype { get; private set; } = "RubberStampLawyer";

    [DataField]
    public string Slot = "pocket1";
}
