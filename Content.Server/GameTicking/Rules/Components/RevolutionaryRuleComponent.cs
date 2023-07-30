using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(RevolutionaryRuleSystem))]

public sealed class RevolutionaryRuleComponent : Component
{
    [DataField("headRevs")]
    public Dictionary<string, string> HeadRevs = new();

    [DataField("shuttleCalled")]
    public bool ShuttleCalled = false;

    [DataField("revsLost")]
    public bool RevsLost = false;

    [DataField("headsDied")]
    public bool HeadsDied = false;

    [DataField("headRevPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string HeadRevPrototypeId = "HeadRev";

    [DataField("headRevGearPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<StartingGearPrototype>))]
    public string HeadRevGearPrototypeId = "HeadRevGear";

    [DataField("revPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string RevPrototypeId = "Rev";


}
