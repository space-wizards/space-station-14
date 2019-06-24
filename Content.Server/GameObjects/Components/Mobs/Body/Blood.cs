namespace Content.Server.GameObjects.Components.Mobs.Body
{
    public class Blood
    {
        public float Volume;

        public Blood(float volume)
        {
            Volume = volume;
        }
        public void changeVolume(float volume)
        {
            Volume += volume;
        }
    }
}
