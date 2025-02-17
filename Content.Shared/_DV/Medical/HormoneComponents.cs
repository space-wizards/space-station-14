using Robust.Shared.GameStates;
using Content.Shared.Humanoid;

namespace Content.Shared._DV.Medical;

public interface IHormoneComponent {
    Sex Target { get; }
    Sex? Original { get; set; }
}

[RegisterComponent, NetworkedComponent]
public sealed partial class MasculinizedComponent : Component, IHormoneComponent {
    public Sex Target => Sex.Male;

    [DataField("original")]
    public Sex? Original { get; set; } = null;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class FeminizedComponent : Component, IHormoneComponent {
    public Sex Target => Sex.Female;

    [DataField("original")]
    public Sex? Original { get; set; } = null;
}
