using Content.Shared.Inventory;
using Content.Shared.StatusEffect;

namespace Content.Shared.Electrocution
{
    public abstract class SharedElectrocutionSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<InsulatedComponent, ElectrocutionAttemptEvent>(OnInsulatedElectrocutionAttempt);
            // as long as legally distinct electric-mice are never added, this should be fine (otherwise a mouse-hat will transfer it's power to the wearer).
            SubscribeLocalEvent<InsulatedComponent, InventoryRelayedEvent<ElectrocutionAttemptEvent>>((e, c, ev) => OnInsulatedElectrocutionAttempt(e, c, ev.Args));
        }

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
        }

        /// <param name="uid">Entity being electrocuted.</param>
        /// <param name="sourceUid">Source entity of the electrocution.</param>
        /// <param name="shockDamage">How much shock damage the entity takes.</param>
        /// <param name="time">How long the entity will be stunned.</param>
        /// <param name="refresh">Should <paramref>time</paramref> be refreshed (instead of accumilated) if the entity is already electrocuted?</param>
        /// <param name="siemensCoefficient">How insulated the entity is from the shock. 0 means completely insulated, and 1 means no insulation.</param>
        /// <param name="statusEffects">Status effects to apply to the entity.</param>
        /// <param name="ignoreInsulation">Should the electrocution bypass the Insulated component?</param>
        /// <returns>Whether the entity <see cref="uid"/> was stunned by the shock.</returns>
        public virtual bool TryDoElectrocution(
            EntityUid uid, EntityUid? sourceUid, int shockDamage, TimeSpan time, bool refresh, float siemensCoefficient = 1f,
            StatusEffectsComponent? statusEffects = null, bool ignoreInsulation = false)
        {
            // only done serverside
            return false;
        }

        private void OnInsulatedElectrocutionAttempt(EntityUid uid, InsulatedComponent insulated, ElectrocutionAttemptEvent args)
        {
            args.SiemensCoefficient *= insulated.Coefficient;
        }
    }
}
