namespace Content.Server.DeadSpace.Abilities.Felinid
{
    [RegisterComponent]
    public sealed partial class HairballComponent : Component
    {
        public string SolutionName = "hairball";

        [DataField]
        public float VomitChance = 0.2f;
    }
}
