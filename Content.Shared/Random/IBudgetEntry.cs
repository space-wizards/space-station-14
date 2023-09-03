namespace Content.Shared.Random;

/// <summary>
/// Budgeted random spawn entry.
/// </summary>
public interface IBudgetEntry : IProbEntry
{
    float Cost { get; }
}

/// <summary>
/// Random entry that has a prob. See <see cref="RandomSystem"/>
/// </summary>
public interface IProbEntry
{
    float Prob { get; }
}

