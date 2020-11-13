using Robust.Shared.Interfaces.Serialization;

namespace Content.Shared.Actions
{
    /// <summary>
    /// Action which requires the user to select a target point, which
    /// does not necessarily have an entity on it.
    /// </summary>
    public interface ITargetPointAction : IActionBehavior
    {

    }
}
