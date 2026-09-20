namespace Content.Shared.Conditions.Satisfier;

public sealed partial class WithBoundary : Satisfier
{
    /// <summary>
    /// The lower value of the boundary
    /// </summary>
   [DataField]
    public float LowerBound { get; set; }

    /// <summary>
    /// If the comparison should include lower bound value
    /// </summary>
   [DataField]
    public bool IncludeLowerBound { get; set; }

    /// <summary>
    /// the upper value of the boundary
    /// </summary>
   [DataField]
    public float UpperBound { get; set; }

    /// <summary>
    /// If the comparison should include higher bound value
    /// </summary>
   [DataField]
    public bool IncludeUpperBound { get; set; }

    protected override bool IsSatisfiedInternal(float value)
    {
        if ((IncludeLowerBound && value < LowerBound) || value <= LowerBound)
            return false;
        if ((IncludeUpperBound && UpperBound < value) || UpperBound <= value)
            return false;
        return true;
    }
}
