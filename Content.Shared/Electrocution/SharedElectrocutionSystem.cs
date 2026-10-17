using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.StatusEffect;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Electrocution;

public abstract partial class SharedElectrocutionSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _timing = default!;

    protected static readonly ProtoId<DamageTypePrototype> DamageType = "Shock";

    public override void Initialize()
    {
        base.Initialize();
        // as long as legally distinct electric-mice are never added, this should be fine (otherwise a mouse-hat will transfer it's power to the wearer).
        SubscribeLocalEvent<InsulatedComponent, InventoryRelayedEvent<ElectrocutionAttemptEvent>>((e, ref ev) => OnInsulatedElectrocutionAttempt(e, ref ev.Args));
    }

    /// <summary>
    /// Tries to set Siemens Coefficient on an entity's insulated component.
    /// </summary>
    public void SetInsulatedSiemensCoefficient(EntityUid uid, float siemensCoefficient, InsulatedComponent? insulated = null)
    {
        if (!Resolve(uid, ref insulated))
            return;

        insulated.Coefficient = siemensCoefficient;
        Dirty(uid, insulated);
    }

    /// <summary>
    /// Sets electrified value of component and marks dirty if required.
    /// </summary>
    public void SetElectrified(Entity<ElectrifiedComponent> ent, bool value)
    {
        if (ent.Comp.Enabled == value)
        {
            return;
        }

        ent.Comp.Enabled = value;
        Dirty(ent, ent.Comp);

        _appearance.SetData(ent.Owner, ElectrifiedVisuals.IsElectrified, value);
    }

    /// <summary>
    /// Set a wire's cut state.
    /// </summary>
    public void SetElectrifiedWireCut(Entity<ElectrifiedComponent> ent, bool value)
    {
        if (ent.Comp.IsWireCut == value)
        {
            return;
        }

        ent.Comp.IsWireCut = value;
        Dirty(ent);
    }

    /// <summary>
    /// Attempts to electrocute an entity interacting with electrified components.
    /// Only call server side.
    /// </summary>
    /// <param name="uid">Entity being electrocuted.</param>
    /// <param name="sourceUid">Source entity of the electrocution.</param>
    /// <param name="shockDamage">How much shock damage the entity takes.</param>
    /// <param name="time">How long the entity will be stunned.</param>
    /// <param name="refresh">Should <paramref>time</paramref> be refreshed (instead of accumulated) if the entity is already electrocuted?</param>
    /// <param name="siemensCoefficient">How insulated the entity is from the shock. 0 means completely insulated, and 1 means no insulation.</param>
    /// <param name="statusEffects">Status effects to apply to the entity.</param>
    /// <param name="ignoreInsulation">Should the electrocution bypass the Insulated component?</param>
    /// <returns>Whether the entity <see cref="uid"/> was stunned by the shock.</returns>
    [PublicAPI]
    public bool TryDoElectrocution(
        EntityUid uid,
        EntityUid? sourceUid,
        FixedPoint2 shockDamage,
        TimeSpan time,
        bool refresh,
        float siemensCoefficient = 1f,
        StatusEffectsComponent? statusEffects = null,
        bool ignoreInsulation = false)
    {
        var damage = new DamageSpecifier(ProtoMan.Index(DamageType), shockDamage);
        return TryDoElectrocution(uid, sourceUid, damage, time, refresh, siemensCoefficient, statusEffects, ignoreInsulation);
    }

    [PublicAPI]
    public virtual bool TryDoElectrocution(
        EntityUid uid,
        EntityUid? sourceUid,
        DamageSpecifier damage,
        TimeSpan time,
        bool refresh,
        float siemensCoefficient = 1f,
        StatusEffectsComponent? statusEffects = null,
        bool ignoreInsulation = false)
    {
        // only done serverside
        return false;
    }

    [SubscribeLocalEvent]
    private static void OnInsulatedElectrocutionAttempt(Entity<InsulatedComponent> ent, ref ElectrocutionAttemptEvent args)
    {
        args.SiemensCoefficient *= ent.Comp.Coefficient;
    }

    [SubscribeLocalEvent]
    private void OnInit(Entity<ElectrifiedComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextShock = _timing.CurTime;
        Dirty(entity);
    }
}
