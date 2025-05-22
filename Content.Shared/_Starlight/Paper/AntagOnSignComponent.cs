using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Paper;

[RegisterComponent, NetworkedComponent]
public sealed partial class AntagOnSignComponent : Component
{
    /// <summary>
    /// has this AntagOnSignComponent been used and either made them a antag or failed to activate due to luck
    /// </summary>
    [ViewVariables] public bool Used = false;
    
    /// <summary>
    /// What is the chance of this signature procing and making them a antag with 1 being always and 0 being never
    /// </summary>
    [DataField] public float Chance = 1.0f;
    
    /// <summary>
    /// should we spawn a paradox clone of the person signing this. technically not making them a antag but it works nearly the same
    /// </summary>
    [DataField("spawnParadoxClone")] public bool ParadoxClone = false;
    
    /// <summary>
    /// what antags should be added to the person.
    /// </summary>
    [DataField] public List<ProtoId<EntityPrototype>> Antags = [];
}