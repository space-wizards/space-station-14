namespace Content.Shared.Random.Rules;

/// <summary>
/// Returns true if on Sweetwater.
/// </summary>
public sealed partial class OnSweetwaterRule : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (entManager.TryGetComponent(uid, out SweetwaterComponent? xform))
        {
            return !Inverted;
        }

        return Inverted;
    }
}
