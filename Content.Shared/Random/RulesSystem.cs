namespace Content.Shared.Random;

public sealed class RulesSystem : EntitySystem
{
    public bool IsTrue(EntityUid uid, RulesPrototype rules)
    {
        foreach (var rule in rules.Rules)
        {
            switch (rule)
            {
                case AlwaysTrueRule:
                    break;
                case InSpaceRule:
                    if (!TryComp<TransformComponent>(uid, out var xform) ||
                        xform.GridUid != null)
                    {
                        return false;
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return true;
    }
}
