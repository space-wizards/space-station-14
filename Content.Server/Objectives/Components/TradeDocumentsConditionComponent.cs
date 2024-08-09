using Content.Server.Objectives.Systems;

namespace Content.Server.Objectives.Components;


/// <summary>
///     Objective that requires you trade your held objective for another trators document. Will not be assigned if there
///     are  no valid traitors to trade with.
/// </summary>
[RegisterComponent]
public sealed partial class TradeDocumentsConditionComponent : Component
{
    [DataField(required: true)]
    public string Title;
    [DataField(required: true)]
    public string Description;
}
