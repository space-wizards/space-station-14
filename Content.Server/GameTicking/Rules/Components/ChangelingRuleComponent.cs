using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Store;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChangelingRuleSystem))]
public sealed partial class ChangelingRuleComponent : Component
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
