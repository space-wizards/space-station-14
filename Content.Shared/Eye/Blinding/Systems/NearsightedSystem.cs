using Content.Shared.Examine;
using Content.Shared.Eye.Blinding;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class NearsightedSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
		SubscribeLocalEvent<NearsightedComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, NearsightedComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && !_net.IsClient)
        {
            args.PushMarkup(Loc.GetString("nearsighted-trait-examined", ("target", Identity.Entity(uid, EntityManager))));
        }
    }
}