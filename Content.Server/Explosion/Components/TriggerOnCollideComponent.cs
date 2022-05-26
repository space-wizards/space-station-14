namespace Content.Server.Explosion.Components
{
    [RegisterComponent]
    public sealed class TriggerOnCollideComponent : Component
    {
		[DataField("fixtureID")]
		public string FixtureID = String.Empty;
    }
}
