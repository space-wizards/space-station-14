using Content.Shared.Examine;
using Content.Shared.Eye.Blinding;
using Content.Shared.IdentityManagement;
using Robust.Shared.Network;

namespace Content.Shared.Traits.Assorted;

/// <summary>
/// This handles permanent blindness, both the examine and the actual effect.
/// </summary>
public sealed class PermanentBlindnessSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedBlindingSystem _blinding = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PermanentBlindnessComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PermanentBlindnessComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PermanentBlindnessComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(EntityUid uid, PermanentBlindnessComponent component, ExaminedEvent args)
    {
        if (args.IsInDetailsRange && !_net.IsClient)
        {
            args.PushMarkup(Loc.GetString("permanent-blindness-trait-examined", ("target", Identity.Entity(uid, EntityManager))));
        }
    }

    private void OnShutdown(EntityUid uid, PermanentBlindnessComponent component, ComponentShutdown args)
    {
        _blinding.AdjustBlindSources(uid, -1);
    }

    private void OnStartup(EntityUid uid, PermanentBlindnessComponent component, ComponentStartup args)
    {
        _blinding.AdjustBlindSources(uid, 1);
    }
}
