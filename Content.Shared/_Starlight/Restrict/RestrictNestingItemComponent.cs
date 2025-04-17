using Content.Shared.Starlight.Antags.Abductor;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Restrict;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class RestrictNestingItemComponent : Component
{
    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1.0);

    [DataField]
    [AutoPausedField]
    public TimeSpan NextPopupTime = TimeSpan.Zero;
}
