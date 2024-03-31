using Content.Shared.Tilenol;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client.Tilenol;

public sealed class ByondSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public bool InitPredict;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ByondComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<ByondComponent, LocalPlayerDetachedEvent>(OnDetached);
        SubscribeLocalEvent<ByondComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ByondComponent, ComponentShutdown>(OnShutdown);
        InitPredict = _cfg.GetCVar(CVars.NetPredict);
    }

    private void OnStartup(EntityUid uid, ByondComponent component, ComponentStartup args)
    {
        if (uid == _player.LocalEntity)
            _cfg.SetCVar(CVars.NetPredict, false);
    }

    private void OnShutdown(EntityUid uid, ByondComponent component, ComponentShutdown args)
    {
        if (uid == _player.LocalEntity)
            _cfg.SetCVar(CVars.NetPredict, InitPredict);
    }

    private void OnDetached(EntityUid uid, ByondComponent component, LocalPlayerDetachedEvent args)
    {
        _cfg.SetCVar(CVars.NetPredict, InitPredict);
    }

    private void OnAttached(EntityUid uid, ByondComponent component, LocalPlayerAttachedEvent args)
    {
        _cfg.SetCVar(CVars.NetPredict, false);
    }

    public void SaveCfg()
    {
        var current = _cfg.GetCVar(CVars.NetPredict);
        _cfg.SetCVar(CVars.NetPredict, InitPredict);
        _cfg.SaveToFile();
        _cfg.SetCVar(CVars.NetPredict, current);
    }
}
