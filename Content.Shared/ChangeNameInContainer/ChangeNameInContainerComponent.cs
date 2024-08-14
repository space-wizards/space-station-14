using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.ChangeNameInContainer;

/// <summary>
///     An entity with this component will get its name chaned to the container it's inside of. E.g, if your a
///     pAI that has this component and are inside a backpack, your name when talking will be "backpack".
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(ChangeNameInContainerSystem))]
public sealed partial class ChangeNameInContainerComponent : Component
{
    /// <summary>
    ///     A whitelist of containers that will change the name. This is so stuff like pockets don't change the name!
    /// </summary>
    [DataField]
    public EntityWhitelist Whitelist = new()
    {
        Components = new[] {"Item"}
    };
}
