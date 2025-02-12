namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed partial class TriggerOnCollideComponent : Component
    {
		[DataField(required: true)]
		public string FixtureID = String.Empty;

        /// <summary>
        ///     Doesn't trigger if the other colliding fixture is nonhard.
        /// </summary>
        [DataField]
        public bool IgnoreOtherNonHard = true;
    }
}
