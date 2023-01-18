namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class TriggerOnCollideComponent : Component
    {
		[DataField("fixtureID", required: true)]
		public string FixtureID = String.Empty;

        /// <summary>
        ///     Doesn't trigger if the other colliding fixture is nonhard.
        /// </summary>
        [DataField("ignoreOtherNonHard")]
        public bool IgnoreOtherNonHard = true;
    }
}
