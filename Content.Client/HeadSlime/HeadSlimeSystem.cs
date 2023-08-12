using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.HeadSlime;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.HeadSlime;

public sealed class HeadSlimeSystem : SharedHeadSlimeSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HeadSlimeComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<HeadSlimeComponent, GetStatusIconsEvent>(OnGetStatusIcon);
    }

    private void OnStartup(EntityUid uid, HeadSlimeComponent component, ComponentStartup args)
    {
        if (HasComp<HumanoidAppearanceComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;
    }
    
    private void OnGetStatusIcon(EntityUid uid, HeadSlimeComponent component, ref GetStatusIconsEvent args)
    {
        if (!HasComp<HeadSlimeComponent>(_player.LocalPlayer?.ControlledEntity))
            return;

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(component.HeadSlimeStatusIcon));
    }
}
