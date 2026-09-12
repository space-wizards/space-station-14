using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.RuntimeFun;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Mind.Components;
using Content.Shared.Speech.Components;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Makes this entity sentient. Allows ghost to take it over if it's not already occupied.
/// Optionally also allows this entity to speak.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class MakeSentientEntityEffectSystem : EntityEffectSystem<MetaDataComponent, MakeSentient>
{
    [Dependency] GhostRoleSystem _ghostRole = default!;
    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<MakeSentient> args)
    {
        // Let affected entities speak normally to make this effect different from, say, the "random sentience" event
        // This also works on entities that already have a mind
        // We call this before the mind check to allow things like player-controlled mice to be able to benefit from the effect
        if (args.Effect.AllowSpeech)
        {
            RemComp<ReplacementAccentComponent>(entity);
            // TODO: Make MonkeyAccent a replacement accent and remove MonkeyAccent code-smell.
            RemComp<MonkeyAccentComponent>(entity);
            RemComp<SpeakOnExceptionComponent>(entity);
        }

        // Stops from adding a ghost role to things like people who already have a mind
        if (TryComp<MindContainerComponent>(entity, out var mindContainer) && mindContainer.HasMind)
            return;

        // Don't add a ghost role to things that already have ghost roles
        if (HasComp<GhostRoleComponent>(entity))
            return;

        _ghostRole.CreateGhostRole(entity.Owner,
            name: entity.Comp.EntityName,
            description: Loc.GetString("ghost-role-information-cognizine-description"),
            rules: Loc.GetString(GhostRoleComponent.DefaultRules));
    }
}
