using Content.Server.Silicons.Laws;
using Content.Shared.Popups;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Silicons.Borgs;

public sealed class BorgConverterSystem : SharedBorgConverterSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SiliconLawSystem _siliconLaw = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgConverterComponent, BorgConversionDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<BorgConverterComponent> ent, ref BorgConversionDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not {} target)
            return;

        var proto = _random.Pick(ent.Comp.Syndiborgs);
        if (TryConvert(target, proto) is not {} borg)
            return;

        _audio.PlayPvs(ent.Comp.Sound, borg);

        Popup.PopupEntity(Loc.GetString(ent.Comp.ConvertedPopup), target, target, PopupType.Medium);

        _siliconLaw.ChangeLawset(borg, ent.Comp.Lawset);
        _siliconLaw.NotifyLawsChanged(borg);
        _siliconLaw.EnsureEmaggedRole(borg);

        var ev = new BorgConvertedEvent(borg);
        RaiseLocalEvent(ent, ref ev);

        // prevent converting multiple borgs
        RemComp<BorgConverterComponent>(ent);
    }
}
