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
        public void SetHandled(SuicideKind kind)
        {
            if (Handled)
                throw new InvalidOperationException("Suicide was already handled");

            Kind = kind;
        }

        public void BlockSuicideAttempt(bool suicideAttempt)
        {
            if (suicideAttempt)
                AttemptBlocked = suicideAttempt;
        }

        public SuicideKind? Kind { get; private set; }
        public EntityUid Victim { get; private set; }
        public bool AttemptBlocked { get; private set; }
        public bool Handled => Kind != null;
    }

    public enum SuicideKind
    {
        Special, //Doesn't damage the mob, used for "weird" suicides like gibbing

        //Damage type suicides
        Blunt,
        Slash,
        Piercing,
        Heat,
        Shock,
        Cold,
        Poison,
        Radiation,
        Asphyxiation,
        Bloodloss
    }
}
