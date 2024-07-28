using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction.Events
{
    /// <summary>
    ///     Raised Directed at an entity to check whether they will handle the suicide.
    /// </summary>
    public sealed class SuicideEvent : EntityEventArgs
    {
        public SuicideEvent(EntityUid victim)
        {
            Victim = victim;
        }
        public void SetHandled(string? kind = null, DamageSpecifier? damage = null)
        {
            if (Handled)
                throw new InvalidOperationException("Suicide was already handled");

            Kind = kind;
            Damage = damage;
            Handled = true;
        }

        public void BlockSuicideAttempt(bool suicideAttempt)
        {
            if (suicideAttempt)
                AttemptBlocked = suicideAttempt;
        }
        public DamageSpecifier? Damage { get; private set; }
        public ProtoId<DamageTypePrototype>? Kind { get; private set; }
        public EntityUid Victim { get; private set; }
        public bool AttemptBlocked { get; private set; }

        public bool Handled;
    }
}
