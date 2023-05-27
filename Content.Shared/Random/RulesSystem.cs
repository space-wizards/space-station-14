namespace Content.Shared.Random;

public sealed class RulesSystem : EntitySystem
{
    public bool IsTrue(RulesPrototype rules)
    {
        foreach (var rule in rules.Rules)
        {
            switch (rule)
            {
                case AlwaysTrueRule:
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        return true;
    }
}
