namespace Content.Shared._Starlight.Railroading;

public interface IRailroadFaxComponent
{
    public HashSet<string> Addresses { get; }

    public List<RailroadFaxLetter> Letters { get; }
}