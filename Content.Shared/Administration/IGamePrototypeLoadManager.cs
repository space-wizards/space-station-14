namespace Content.Shared.Administration;

public interface IGamePrototypeLoadManager
{
    public void Initialize();
    public void SendGamePrototype(string prototype);

    event Action GamePrototypeLoaded;
}
