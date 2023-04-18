namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent]
public sealed class PiratesRuleComponent : Component
{
    [ViewVariables]
    public List<Mind.Mind> Pirates = new();
    [ViewVariables]
    public EntityUid PirateShip = EntityUid.Invalid;
    [ViewVariables]
    public HashSet<EntityUid> InitialItems = new();
    [ViewVariables]
    public double InitialShipValue;

}
