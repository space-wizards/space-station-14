using Robust.Shared.GameStates;

namespace Content.Shared.Chasm;

/// <summary>
///     Marks a component that will cause entities to fall into them on a step trigger activation
/// </summary>
[NetworkedComponent, RegisterComponent, Access(typeof(ChasmSystem))]
public sealed class ChasmComponent : Component
{
}
