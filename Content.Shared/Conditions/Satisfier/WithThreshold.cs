namespace Content.Shared.Conditions.Satisfier;

public sealed partial class WithThreshold : Satisfier
{
    /// <summary>
    /// Standard mathematically comparators
    /// </summary>
    public enum Comparator
    {
        Less,
        LessEqual,
        Equal,
        Greater,
        GreaterEqual,
    }

    /// <summary>
    /// Which comparison is to be used?
    /// Hint: [Value of Condition] [<see cref="Comparison" />] [<see cref="Threshold" />]
    /// </summary>
    [DataField]
    public Comparator Comparison { get; set; }

    /// <summary>
    /// The Value against to compare
    /// </summary>
    [DataField]
    public float Threshold { get; set; }

    /// <inheritdoc />
    protected override bool IsSatisfiedInternal(float value)
    {
        switch (Comparison)
        {
            case Comparator.Less:
                return value < Threshold;
            case Comparator.LessEqual:
                return value <= Threshold;
            case Comparator.Equal:
                return value.Equals(Threshold);
            case Comparator.Greater:
                return value > Threshold;
            case Comparator.GreaterEqual:
                return value >= Threshold;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
