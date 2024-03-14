using Content.Shared.Mobs;
using Content.Server.Chat;
using Content.Server.Chat.Systems;

namespace Content.Server.Fart;

public sealed class FartSystem : EntitySystem
{
    [Dependency] private readonly AutoEmoteSystem _autoEmote = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FartComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<FartComponent, MobStateChangedEvent>(OnMobState);
    }

    private void OnMobState(EntityUid uid, FartComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            RemComp<AutoEmoteComponent>(uid);
    }

    private void OnComponentStartup(EntityUid uid, FartComponent component, ComponentStartup args)
    {
        EnsureComp<AutoEmoteComponent>(uid);
        _autoEmote.AddEmote(uid, "FartAuto");
    }
}
