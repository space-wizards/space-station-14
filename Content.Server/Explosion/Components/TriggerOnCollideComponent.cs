namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class TriggerOnCollideComponent : Component
    {
		[DataField("fixtureID", required: true)]
		public string FixtureID = String.Empty;
    }
}
