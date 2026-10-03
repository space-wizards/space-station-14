namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// Tag a Condition to add inversion to the boolean evaluation.
/// Is applied at the end of all other evaluations of the condition once.
/// Technically a condition can both implement this and have <see cref="WithInverted" />
/// </summary>
public interface IWithInverted
{
    bool Inverted { get; }
}
