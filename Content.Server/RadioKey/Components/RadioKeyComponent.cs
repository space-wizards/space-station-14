using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using System.Linq;
using Content.Shared.Radio;

namespace Content.Server.RadioKey.Components;

/// <summary>
/// The thing that HOLDS radiokey
/// </summary>
[RegisterComponent]
public sealed class RadioKeyComponent : Component
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <summary>
    /// List of "radiokeys" this has. The component is a holder for the radio keys (prototypes). Maximum of 2
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("radioKeyPrototype", customTypeSerializer: typeof(PrototypeIdListSerializer<RadioKeyPrototype>))]
    public List<string> RadioKeyPrototype = new();

    /// <summary>
    /// The frequency this unlocks
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public readonly HashSet<int> UnlockedFrequency = new();

    /// <summary>
    /// The frequency we blocked. Yes, it is stored in the radiokey, not the radio.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public readonly HashSet<int> BlockedFrequency = new();

    // TODO flag these or something
    [ViewVariables]
    public bool Syndie { get; private set; }

    [ViewVariables]
    public bool TranslateBinary { get; private set; }

    protected override void Initialize()
    {
        base.Initialize();
        if (RadioKeyPrototype.Count > 2)
        {
            // WHAT ARE YOU DOING
            Logger.Warning("RadioKeyComponent has more than 2 RadioKeyPrototypes.");
        }

        UpdateFrequencies();
    }

    /// <summary>
    /// call this when you add or remove radio keys.
    /// </summary>
    public void UpdateFrequencies()
    {
        Syndie = false;
        TranslateBinary = false;
        UnlockedFrequency.Clear();
        List<int> newFreqTx = new();

        foreach (var item in RadioKeyPrototype)
        {
            if (!_prototypeManager.TryIndex<RadioKeyPrototype>(item, out var radioKeyPrototype)) continue;
            newFreqTx = newFreqTx.Union(radioKeyPrototype.Frequency).ToList();
            if (!Syndie && radioKeyPrototype.Syndie)
            {
                Syndie = true;
            }
            if (!TranslateBinary && radioKeyPrototype.TranslateBinary)
            {
                TranslateBinary = true;
            }
        }

        var radioSys = EntitySystem.Get<SharedRadioSystem>();
        foreach (var freq in newFreqTx)
        {
            // TODO just sanitize it on the prototype stupid
            if (!radioSys.IsOutsideFreeFreq(freq))
                continue; // dont let pubby channels get in here (exept ai sat ones?)
            UnlockedFrequency.Add(radioSys.SanitizeFrequency(freq, true));
        }
    }
}
