// SPDX-FileCopyrightText: 2025 Drywink <hugogrethen@gmail.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Species;

/// <summary>
/// Component that allows an entity to bite targets and inject solutions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BiteComponent : Component
{
    /// <summary>
    /// The reagents to inject when biting, as a dictionary of reagent ID to amount.
    /// </summary>
    [DataField("injectedReagents"), AutoNetworkedField]
    public Dictionary<string, FixedPoint2> InjectedReagents = new();

    /// <summary>
    /// The cooldown time for the bite ability in seconds.
    /// </summary>
    [DataField("cooldown"), AutoNetworkedField]
    public float Cooldown = 5f;

    /// <summary>
    /// The action entity for the bite ability.
    /// </summary>
    [DataField("actionEntity"), AutoNetworkedField]
    public EntityUid? ActionEntity;

    /// <summary>
    /// The prototype ID of the bite action.
    /// </summary>
    [DataField("biteAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>)), AutoNetworkedField]
    public string BiteAction = "ActionBite";

    /// <summary>
    /// Optional custom description for the bite action. If null, uses the default from the action prototype.
    /// </summary>
    [DataField("actionDescription"), AutoNetworkedField]
    public LocId? ActionDescription;
}

