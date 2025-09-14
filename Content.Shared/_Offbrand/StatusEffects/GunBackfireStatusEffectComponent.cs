/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using Robust.Shared.Audio;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(GunBackfireStatusEffectSystem))]
public sealed partial class GunBackfireStatusEffectComponent : Component
{
    [DataField]
    public float Probability = 0.2f;

    [DataField]
    public TimeSpan BackfireStunTime = TimeSpan.FromSeconds(3);

    [DataField]
    public SoundSpecifier BackfireSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/bang.ogg");

    [DataField]
    public LocId BackfireMessage = "backfired-gun";
}
