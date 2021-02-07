namespace Content.Server.AI.Utility.Curves
{
    /// <summary>
    /// Using an interface also lets us define preset curves that can be re-used
    /// </summary>
    public interface IResponseCurve
    {
        float GetResponse(float score);
    }
}
