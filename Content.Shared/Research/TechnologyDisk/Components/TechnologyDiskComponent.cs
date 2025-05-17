﻿using Content.Shared.Random;
using Content.Shared.Research.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Research.TechnologyDisk.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class TechnologyDiskComponent : Component
{
    /// <summary>
    /// A discipline to constrain the disk to.
    /// </summary>
    [DataField]
    public ProtoId<TechDisciplinePrototype> Discipline;

    /// <summary>
    /// A tier to constrain the disk to.
    /// </summary>
    [DataField]
    public int Tier;

    /// <summary>
    /// The recipe that will be added. If null, one will be randomly generated
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public List<ProtoId<LatheRecipePrototype>>? Recipes;
}
