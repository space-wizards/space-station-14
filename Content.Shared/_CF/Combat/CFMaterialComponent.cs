using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Combat;

/// <summary>
/// Explicit Pathfinder material on an entity. When absent, <see cref="CFCombatBridgeSystem"/> infers one.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFMaterialComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<CFMaterialPrototype> Material = "Wood";
}
