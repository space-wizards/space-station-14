using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Interaction.Events
{
    /// <summary>
    ///     Raised Directed at an entity to check whether they will handle the suicide.
    /// </summary>
    public sealed class SuicideEvent : HandledEntityEventArgs
    {
        public SuicideEvent(EntityUid victim)
        {
            Victim = victim;
        }

        public void BlockSuicideAttempt(bool suicideAttempt)
        {
            if (suicideAttempt)
                AttemptBlocked = suicideAttempt;
        }
        public DamageSpecifier? Damage;

        public ProtoId<DamageTypePrototype>? Kind;
        public EntityUid Victim { get; private set; }
        public bool AttemptBlocked { get; private set; }
    }
}
