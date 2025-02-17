// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.DeadSpace.Abilities.Invisibility.Components;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.DeadSpace.Abilities.Invisibility;

public abstract class SharedInvisibilitySystem : EntitySystem
{
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public void TogleInvisibility(EntityUid uid, InvisibilityComponent component)
    {
        component.IsInvisible = !component.IsInvisible;

        if (!component.IsInvisible)
        {
            RemComp<StealthComponent>(uid);
            return;
        }

        var stealth = EnsureComp<StealthComponent>(uid);

        var visibility = component.IsInvisible ? component.MinVisibility + component.Visibility : component.MaxVisibility;

        _stealth.SetVisibility(uid, visibility, stealth);

        if (component.InvisibilitySound == null)
        {
            return;
        }

        _audio.PlayPvs(component.InvisibilitySound, uid, AudioParams.Default.WithVolume(3));
    }
}
