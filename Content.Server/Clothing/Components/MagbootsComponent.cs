using Content.Shared.Clothing;

namespace Content.Server.Clothing.Components;

[RegisterComponent]
[ComponentReference(typeof(SharedMagbootsComponent))]
public sealed class MagbootsComponent : SharedMagbootsComponent
{
    [ViewVariables]
    public override bool On { get; set; }

    public override ComponentState GetComponentState()
    {
        return new MagbootsComponentState(On);
    }
}
