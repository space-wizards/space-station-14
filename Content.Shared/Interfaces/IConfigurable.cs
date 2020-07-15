namespace Content.Shared.Interfaces
{
    public interface IConfigurable<in T>
    {
        public void Configure(T parameters);
    }
}
