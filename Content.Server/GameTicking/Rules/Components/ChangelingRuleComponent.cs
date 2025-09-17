using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Gamerule component for handling a changeling antagonist.
/// </summary>
[RegisterComponent]
public sealed partial class ChangelingRuleComponent : Component;

#region Starlight. THIS SHOULD REALLY BE PUT INTO _Starlight WHY IS IT NOT
[RegisterComponent, Access(typeof(ChangelingRuleSystem))]
public sealed partial class SLChangelingRuleComponent : Component
{
    public readonly List<EntityUid> ChangelingMinds = new();

    public readonly List<ProtoId<StoreCategoryPrototype>> StoreCategories = new()
    {
        "ChangelingAbilityCombat",
        "ChangelingAbilitySting",
        "ChangelingAbilityUtility"
    };

    public readonly List<ProtoId<EntityPrototype>> Objectives = new()
    {
        "ChangelingSurviveObjective",
        "ChangelingStealDNAObjective",
        "EscapeIdentityObjective"
    };
}
#endregion
