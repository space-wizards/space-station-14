using Content.Shared.CF.Character;

namespace Content.Client.CF.Character;

/// <summary>One spell the open character sheet can cast or prepare.</summary>
public sealed class CFSheetSpell
{
    public string Id = "";
    public string Name = "";
    public int Rank;
    public string Defense = "";
    public string Range = "";
    public CFSpellActionCost ActionCost;
    public EntityUid Action;
    public bool Ritual;
    public bool Activation;
}
