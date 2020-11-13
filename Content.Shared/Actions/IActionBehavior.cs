using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Currently just a marker interface delineating the different possible
    /// types of action behaviors.
    /// </summary>
    public interface IActionBehavior : IExposeData
    {

    }
}
