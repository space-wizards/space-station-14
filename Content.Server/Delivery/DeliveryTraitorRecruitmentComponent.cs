using Content.Shared.Antag;
using Robust.Shared.Prototypes;

namespace Content.Server.Delivery;

/// <summary>
/// Marks a delivery letter as a Syndicate recruitment letter. When the
/// addressed recipient opens it, they are made a traitor and the enclosed
/// paper self-destructs.
/// </summary>
[RegisterComponent, Access(typeof(DeliveryTraitorRecruitmentSystem))]
public sealed partial class DeliveryTraitorRecruitmentComponent : Component
{
    /// <summary>
    /// Dedicated game rule so mail recruitment stays out of normal antag budgets.
    /// </summary>
    [DataField]
    public EntProtoId RulePrototype = "TraitorByMail";

    [DataField]
    public ProtoId<AntagSpecifierPrototype> AntagProto = "TraitorSleeper";
}
