// Initial file ported from the Starlight project repo, located at https://github.com/ss14Starlight/space-station-14

using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.VentCraw;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VentCrawlerComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public bool InTube = false;

    [DataField, ViewVariables, AutoNetworkedField]
    public TimeSpan EnterDelay = TimeSpan.FromSeconds(5);
}


[Serializable, NetSerializable]
public sealed partial class EnterVentDoAfterEvent : SimpleDoAfterEvent
{
}
