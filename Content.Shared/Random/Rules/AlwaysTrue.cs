namespace Content.Shared.Random.Rules;

/// <summary>
/// Always returns true. Used for fallbacks.
/// </summary>
public sealed partial class AlwaysTrueRule : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        return !Inverted;
    }
}
