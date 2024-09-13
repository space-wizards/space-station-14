using System.Threading;
using Content.Server.Revenant.EntitySystems;
using Content.Shared.Revenant.Components;
using Robust.Shared.GameStates;

namespace Content.Server.Revenant.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RevenantAnimatedSystem))]
public sealed partial class RevenantAnimatedComponent : Component
{
    /// <summary>
    /// The revenant that animated this item. Used for initialization.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Entity<RevenantComponent>? Revenant;

    /// <summary>
    /// Components added to make this item animated.
    /// Removed when the item becomes inanimate.
    /// </summary>
    public List<Component> AddedComponents = new();

    public CancellationTokenSource CancelToken;
}