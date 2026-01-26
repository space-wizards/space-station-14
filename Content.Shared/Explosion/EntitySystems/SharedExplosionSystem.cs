using Content.Shared.Armor;
using Content.Shared.Explosion.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Explosion.EntitySystems;

// TODO some sort of struct like DamageSpecifier but for explosions.
/// <summary>
/// Lets code in shared trigger explosions and handles explosion resistance examining.
/// All processing is still done clientside.
/// </summary>
public abstract class SharedExplosionSystem : EntitySystem
{
    /// <summary>
    ///     The "default" explosion prototype.
    /// </summary>
    /// <remarks>
    ///     Generally components should specify an explosion prototype via a yaml datafield, so that the yaml-linter can
    ///     find errors. However some components, like rogue arrows, or some commands like the admin-smite need to have
    ///     a "default" option specified outside of yaml data-fields. Hence this const string.
    /// </remarks>
    public static readonly ProtoId<ExplosionPrototype> DefaultExplosionPrototypeId = "Default";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ExplosionResistanceComponent, ArmorExamineEvent>(OnArmorExamine);
    }

    private void OnArmorExamine(Entity<ExplosionResistanceComponent> ent, ref ArmorExamineEvent args)
    {
        var value = MathF.Round((1f - ent.Comp.DamageCoefficient) * 100, 1);

        if (value == 0)
            return;

        args.Msg.PushNewline();
        args.Msg.AddMarkupOrThrow(Loc.GetString(ent.Comp.Examine, ("value", value)));
    }

    /// <summary>
    ///     Given an entity with an explosive component, spawn the appropriate explosion.
    /// </summary>
    /// <remarks>
    ///     Also accepts radius or intensity arguments. This is useful for explosives where the intensity is not
    ///     specified in the yaml / by the component, but determined dynamically (e.g., by the quantity of a
    ///     solution in a reaction).
    /// </remarks>
    public virtual void TriggerExplosive(EntityUid uid, ExplosiveComponent? explosive = null, bool delete = true, float? totalIntensity = null, float? radius = null, EntityUid? user = null)
    {
    }

    /// <summary>
    /// Queue an explosion centered on some entity. Bypasses needing <see cref="ExplosiveComponent"/>.
    /// </summary>
    /// <param name="uid">Where the explosion happens.</param>
    /// <param name="typeId">A ProtoId of type <see cref="ExplosionPrototype"/>.</param>
    /// <param name="user">The entity which caused the explosion.</param>
    /// <param name="addLog">Whether to add an admin log about this explosion. Includes user.</param>
    public virtual void QueueExplosion(EntityUid uid,
                                        string typeId,
                                        float totalIntensity,
                                        float slope,
                                        float maxTileIntensity,
                                        float tileBreakScale = 1f,
                                        int maxTileBreak = int.MaxValue,
                                        bool canCreateVacuum = true,
                                        EntityUid? user = null,
                                        bool addLog = true)
    {
    }

    /// <summary>
    /// This forces the explosion system to re-calculate the explosion intensity required to destroy all airtight entities.
    /// </summary>
    public virtual void ReloadMap()
    {
    }
}
