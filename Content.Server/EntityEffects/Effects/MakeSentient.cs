using System.Linq;
using Content.Server._EinsteinEngine.Language;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Speech.Components;
using Content.Shared.EntityEffects;
using Content.Shared._EinsteinEngine.Language;
using Content.Shared._EinsteinEngine.Language.Components;
using Content.Shared._EinsteinEngine.Language.Systems;
using Content.Shared.Mind.Components;
using Content.Shared._EinsteinEngine.Language.Events;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

public sealed partial class MakeSentient : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-make-sentient", ("chance", Probability));

    public override void Effect(EntityEffectBaseArgs args)
    {
        var entityManager = args.EntityManager;
        var uid = args.TargetEntity;

        // Let affected entities speak normally to make this effect different from, say, the "random sentience" event
        // This also works on entities that already have a mind
        // We call this before the mind check to allow things like player-controlled mice to be able to benefit from the effect
        entityManager.RemoveComponent<ReplacementAccentComponent>(uid);
        entityManager.RemoveComponent<MonkeyAccentComponent>(uid);

        var speaker = entityManager.EnsureComponent<LanguageSpeakerComponent>(uid);
        var knowledge = entityManager.EnsureComponent<LanguageKnowledgeComponent>(uid);
        var fallback = SharedLanguageSystem.FallbackLanguagePrototype;

        if (!knowledge.UnderstoodLanguages.Contains(fallback))
            knowledge.UnderstoodLanguages.Add(fallback);

        if (!knowledge.SpokenLanguages.Contains(fallback))
            knowledge.SpokenLanguages.Add(fallback);

        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<LanguageSystem>().UpdateEntityLanguages(uid);

        // Stops from adding a ghost role to things like people who already have a mind
        if (entityManager.TryGetComponent<MindContainerComponent>(uid, out var mindContainer) && mindContainer.HasMind)
        {
            return;
        }

        // Don't add a ghost role to things that already have ghost roles
        if (entityManager.TryGetComponent(uid, out GhostRoleComponent? ghostRole))
        {
            return;
        }

        ghostRole = entityManager.AddComponent<GhostRoleComponent>(uid);
        entityManager.EnsureComponent<GhostTakeoverAvailableComponent>(uid);

        var entityData = entityManager.GetComponent<MetaDataComponent>(uid);
        ghostRole.RoleName = entityData.EntityName;
        ghostRole.RoleDescription = Loc.GetString("ghost-role-information-cognizine-description");
    }
}
