using System;
using System.Runtime;
using Content.Server.Chat.Managers;
using Content.Shared.DeadSpace.CCCCVars;
using Content.Shared.Chat;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.AutoGCF;

/// <summary>
///     Handles periodically GCF (Garbage Collector)
/// </summary>
public sealed class GCFSystem : EntitySystem
{
    [Dependency] private readonly IChatManager _chat = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private bool _gcfEnabled;
    private bool _gcfNotify;
    private float _gcfTime;

    [ViewVariables(VVAccess.ReadWrite)]
    private TimeSpan _nextGCFTime = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(CCCCVars.GCFFrequency, SetTimeGCF, true);
        _cfg.OnValueChanged(CCCCVars.GCFEnabled, SetEnabledGCF, true);
        _cfg.OnValueChanged(CCCCVars.GCFNotify, SetEnabledNotify, true);

        RecalculateNextGCFTime();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_gcfEnabled)
            return;

        if (_nextGCFTime != TimeSpan.Zero && _timing.CurTime > _nextGCFTime)
        {
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, true, true);
            AnnounceGCF();
            RecalculateNextGCFTime();
        }
    }

    private void SetTimeGCF(float value)
    {
        _gcfTime = value;
    }

    private void SetEnabledGCF(bool value)
    {
        _gcfEnabled = value;

        if (_nextGCFTime != TimeSpan.Zero)
            RecalculateNextGCFTime();
    }

    private void SetEnabledNotify(bool value)
    {
        _gcfNotify = value;
    }

    private void AnnounceGCF()
    {
        if (!_gcfNotify)
            return;
        _chat.SendAdminAnnouncement("Автоочистка завершена успешно");
    }

    private void RecalculateNextGCFTime()
    {
        _nextGCFTime = _timing.CurTime + TimeSpan.FromSeconds(_gcfTime);
    }
}