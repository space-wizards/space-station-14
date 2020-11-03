namespace Content.Shared.GameObjects.Components.Mobs.State
{
    /// <summary>
    ///     The standard state an entity is in; no negative effects.
    /// </summary>
    public abstract class SharedNormalState : MobState
    {
        public override bool CanInteract()
        {
            return true;
        }

        public override bool CanMove()
        {
            return true;
        }

        public override bool CanUse()
        {
            return true;
        }

        public override bool CanThrow()
        {
            return true;
        }

        public override bool CanSpeak()
        {
            return true;
        }

        public override bool CanDrop()
        {
            return true;
        }

        public override bool CanPickup()
        {
            return true;
        }

        public override bool CanEmote()
        {
            return true;
        }

        public override bool CanAttack()
        {
            return true;
        }

        public override bool CanEquip()
        {
            return true;
        }

        public override bool CanUnequip()
        {
            return true;
        }

        public override bool CanChangeDirection()
        {
            return true;
        }
    }
}
