namespace Content.Shared.Conditions.HelperConditions;

/// <summary>
/// Tag a Condition to add inversion to the boolean evaluation.
/// Is applied at the end of all other evaluations of the condition once.
/// </summary>
public interface IWithInverted
{
    bool Inverted { get; }
}
