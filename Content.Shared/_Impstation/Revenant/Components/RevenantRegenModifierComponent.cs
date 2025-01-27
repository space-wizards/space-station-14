using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Revenant.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RevenantRegenModifierComponent : Component
{
    [DataField(required: true), ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> Witnesses;

    [DataField(required: true), ViewVariables, AutoNetworkedField]
    public int NewHaunts;

    [DataField]
    public ProtoId<AlertPrototype> Alert = "EssenceRegen";

    public RevenantRegenModifierComponent(HashSet<NetEntity> witnesses, int newHaunts)
    {
        Witnesses = witnesses;
        NewHaunts = newHaunts;
    }

    public RevenantRegenModifierComponent() : this(new(), 0)
    {
    }
}
