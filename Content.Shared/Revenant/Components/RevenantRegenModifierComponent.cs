using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevenantRegenModifierComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> Witnesses;

    [DataField]
    public ProtoId<AlertPrototype> Alert = "EssenceRegen";

    public RevenantRegenModifierComponent(HashSet<NetEntity> witnesses)
    {
        Witnesses = witnesses;
    }

    public RevenantRegenModifierComponent() : this(new())
    {
    }
}