using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Medical.Limbs;
public partial interface IWithAction : IComponent
{
    public bool EntityIcon { get; } // It shouldn't be here, but I’m too lazy to redo everything.

    public EntProtoId Action { get; }

    public EntityUid? ActionEntity { get; set; }
}
