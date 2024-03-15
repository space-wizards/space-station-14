using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Silicons.Laws;

/// <inheritdoc/>
public sealed class SiliconLawSystem : SharedSiliconLawSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanDisplayStatusIconsEvent>(OnCanDisplayStatusIcons);
    }

    private void OnCanDisplayStatusIcons(ref CanDisplayStatusIconsEvent args)
    {
        if (HasComp<SiliconLawBoundComponent>(args.User))
            args.Cancelled = true;
    }
}
