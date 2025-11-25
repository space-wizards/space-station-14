using Content.Shared.Access.Systems;

namespace Content.Shared.Interaction.Requirements;
public sealed class ActivatableUIRequiresAccessSystem : InteractionRequirementSystem<InteractionRequirementAccessComponent>
{
    [Dependency] private readonly AccessReaderSystem _access = default!;

    protected override string FailureSuffix => "access";

    protected override bool Condition(Entity<InteractionRequirementAccessComponent> ent, ref readonly ConditionalInteractionAttemptEvent args)
    {
        if (_access.IsAllowed(args.Source, ent))
            return true;

        return false;
    }
}

