using Content.Shared.DeadSpace.BSAA;
using Content.Shared.DeadSpace.Necromorphs.InfectionDead.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.DeadSpace.BSAA;

public sealed class BSAASpecialistSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly SpriteSystem _sprites = default!;

    private readonly Dictionary<EntityUid, bool> _originalVisibility = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BSAASpecialistComponent, ComponentInit>(OnSpecialistInit);
        SubscribeLocalEvent<BSAASpecialistComponent, ComponentShutdown>(OnSpecialistShutdown);
        SubscribeLocalEvent<NecromorfComponent, ComponentInit>(OnNecromorphChanged);
        SubscribeLocalEvent<NecromorfComponent, ComponentShutdown>(OnNecromorphChanged);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnSpecialistInit(Entity<BSAASpecialistComponent> ent, ref ComponentInit args)
    {
        UpdateSpecialist(ent);
    }

    private void OnSpecialistShutdown(Entity<BSAASpecialistComponent> ent, ref ComponentShutdown args)
    {
        Restore(ent.Owner);
    }

    private void OnNecromorphChanged(Entity<NecromorfComponent> ent, ref ComponentInit args)
    {
        if (_players.LocalEntity == ent.Owner)
            UpdateAll();
    }

    private void OnNecromorphChanged(Entity<NecromorfComponent> ent, ref ComponentShutdown args)
    {
        if (_players.LocalEntity == ent.Owner)
            UpdateAll(false);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        UpdateAll();
    }

    private void UpdateAll(bool? seesAsNecromorph = null)
    {
        var isNecromorph = seesAsNecromorph ?? HasComp<NecromorfComponent>(_players.LocalEntity);
        var query = EntityQueryEnumerator<BSAASpecialistComponent>();
        while (query.MoveNext(out var uid, out var component))
            UpdateSpecialist((uid, component), isNecromorph);
    }

    private void UpdateSpecialist(Entity<BSAASpecialistComponent> ent, bool? seesAsNecromorph = null)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var isNecromorph = seesAsNecromorph ?? HasComp<NecromorfComponent>(_players.LocalEntity);
        if (!isNecromorph)
        {
            Restore(ent.Owner, sprite);
            return;
        }

        if (!_originalVisibility.ContainsKey(ent.Owner))
            _originalVisibility[ent.Owner] = sprite.Visible;

        _sprites.SetVisible((ent.Owner, sprite), false);
    }

    private void Restore(EntityUid uid, SpriteComponent? sprite = null)
    {
        if (!_originalVisibility.Remove(uid, out var visible))
            return;

        if (Resolve(uid, ref sprite, false))
            _sprites.SetVisible((uid, sprite), visible);
    }
}
