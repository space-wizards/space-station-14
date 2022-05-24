namespace Content.Server.Zombies
{
    [RegisterComponent]
    public sealed class ZombifyOnDeathComponent : Component
    {
        [DataField("skinColor")]
        public Color SkinColor = new Color(0.70f, 0.72f, 0.48f, 1);
        
        public bool Zombified = false;
    }
}
