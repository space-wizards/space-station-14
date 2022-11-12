using System.Runtime.InteropServices;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Prototypes;
using Content.Shared.Medical.Wounds.Systems;

namespace Content.Shared.Medical.Wounds;

public readonly ref struct WoundHandle
{
    public readonly string Prototype = string.Empty;
    public readonly int WoundIndex = -1;
    private readonly WoundableComponent? parentContainer = null;
    public WoundableComponent Parent
    {
        get
        {
            if (parentContainer == null)
                throw new ArgumentException("Tried to get the parent of an invalid WoundId");
            return parentContainer;
        }
    }

    public bool Valid => parentContainer != null;
    public WoundHandle(WoundableComponent woundable, string prototype, int woundIndex)
    {
        if (woundIndex < 0 || !woundable.Wounds.ContainsKey(Prototype) ||
            woundable.Wounds[Prototype].Count >= WoundIndex)
            return; //Do not construct a valid woundId if any of the construction args are invalid
        Prototype = prototype;
        parentContainer = woundable;
        WoundIndex = woundIndex;

    }
}
