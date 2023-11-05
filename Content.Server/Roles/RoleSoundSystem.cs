using Content.Shared.Mind;
using Content.Shared.Roles;
using Robust.Shared.Audio;

namespace Content.Server.Roles;

public sealed class RoleSoundSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoleSoundComponent, MindRoleAddedEvent>(OnAdded);
    }

    private void OnAdded(Entity<RoleSoundComponent> ent, ref MindRoleAddedEvent args)
    {
        if (!args.Silent && ent.Comp.Sound != null && _mind.TryGetSession(ent, out var session))
            _audio.PlayGlobal(ent.Comp.Sound, session);
    }
}
