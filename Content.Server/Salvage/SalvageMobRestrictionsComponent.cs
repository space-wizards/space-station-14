using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;
using System;

namespace Content.Server.Salvage;

/// <summary>
///     This component exists as a sort of stateful marker for a
///     killswitch meant to keep salvage mobs from doing stuff they
///     really shouldn't (attacking station).
///     The main thing is that this component receives critical events that alert SalvageMobRestrictionsSystem to the presence of a 
/// </summary>
[RegisterComponent]
public sealed class SalvageMobRestrictionsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [DataField("linkedGridEntity")]
    public EntityUid LinkedGridEntity = EntityUid.Invalid;
}

