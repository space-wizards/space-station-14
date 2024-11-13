using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Xenoarchaeology.Artifact.XAT.Components;

/// <summary>
/// This is used for a xenoarch trigger that activates when a reaction occurs on the artifact.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(XATReactiveSystem)), AutoGenerateComponentState]
public sealed partial class XATReactiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ReactionMethod> ReactionMethods = new() { ReactionMethod.Touch };

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<ReagentPrototype>> Reagents = new();

    //todo: ReactiveGroupPrototype

    [DataField, AutoNetworkedField]
    public FixedPoint2 MinQuantity = 5f;
}
