# Abilities
changeling-biomass-deficit = We do not have enough biomass.
changeling-chemicals-deficit = We do not have enough chemicals.
changeling-action-fail-lesserform = We cannot use this in our lesser form.
changeling-action-fail-absorbed = We must absorb {$number} more organics to use this.

changeling-examine-reduced-biomass = [color=yellow]{CAPITALIZE(SUBJECT($target))} will not be enough to satisfy us.[/color]
changeling-examine-rotting = [color=yellow]{CAPITALIZE(POSS-ADJ($target))} corpse will not be enough to satisfy us.[/color]
changeling-examine-extremely-bloated = [color=red]{CAPITALIZE(POSS-ADJ($target))} corpse is inedible.[/color]

changeling-absorb-start-self = We prepare to consume {THE($target)}.
changeling-absorb-start-target = You feel a sharp stabbing pain!
changeling-absorb-start-others = {CAPITALIZE($user)} hunches over {THE($target)}!
changeling-absorb-fail-incapacitated = We cannot consume {THE($target)} until {SUBJECT($target)} {CONJUGATE-BE($target)} incapacitated.
changeling-absorb-fail-absorbed = We have already consumed {THE($target)}.
changeling-absorb-fail-unabsorbable = We cannot consume {THE($target)}.
changeling-absorb-fail-extremely-bloated = {CAPITALIZE(THE($target))} is too rotten to consume.
changeling-absorb-end-self = We have consumed {THE($target)}.
changeling-absorb-end-self-ling = We have consumed another changeling. We are evolving more rapidly.
changeling-absorb-end-self-reduced-biomass = We have consumed {THE($target)}, but {SUBJECT($target)} {CONJUGATE-BE($target)} not enough to satisfy us.
changeling-absorb-end-target = You are consumed by the changeling!
changeling-absorb-end-others = {CAPITALIZE(THE($user))} hollows out {THE($target)}!
changeling-absorb-onexamine = [color=red]{CAPITALIZE(POSS-ADJ($target))} body is hollow.[/color]

changeling-transform-cycle = Switched to {$target}'s DNA.
changeling-transform-cycle-empty = We have no DNA strains.
changeling-transform-self = We assume {THE($target)}'s form.
changeling-transform-target = Your body contorts into the shape of {THE($target)}!
changeling-transform-others = {CAPITALIZE($user)}'s body contorts into the shape of {THE($target)}!
changeling-transform-lesser-self = We assume a lesser form.
# TODO: different lesser form species?
changeling-transform-lesser-others = {CAPITALIZE($user)}'s body contorts into the shape of a monkey!
changeling-transform-fail-self = We cannot re-assume our current form.
changeling-transform-fail-choose = We must select a form to assume.
changeling-transform-fail-absorbed = We cannot transform a hollow body.

changeling-sting-fail-self = We tried to stealthily sting {THE($target)}, but our attempt failed.
changeling-sting-fail-ling = We felt something attempt to sting us.
changeling-sting-fail-simplemob = We cannot sting a lesser creature.

changeling-sting-self = We silently sting {THE($target)}.
changeling-sting-target = You feel a tiny prick.
changeling-sting-extract-fail = {CAPITALIZE(THE($target))} lacks extractable DNA.
changeling-sting-extract-max = We cannot extract more DNA until we assume a new form.

changeling-stasis-enter = We enter regenerative stasis.
changeling-stasis-enter-fail = We cannot enter stasis.
changeling-stasis-exit = We rise from stasis.
changeling-stasis-exit-fail = We are not in stasis.
changeling-stasis-exit-fail-dead = We have been hollowed, and cannot rise from stasis.

changeling-fail-hands = We require a free hand.

changeling-muscles-start = Our body feels lighter.
changeling-muscles-end = Our body regains its weight.

changeling-equip-armor-fail = We must remove our outer clothing.

changeling-inject = We inject ourself.
changeling-inject-fail = We cannot inject ourself.

changeling-passive-activate = We activate an ability.
changeling-passive-activate-fail = We cannot activate this ability.
changeling-passive-active = This ability is already active.

changeling-fleshmend = Our body twists, sealing wounds and regenerating dead cells.
changeling-panacea = We mutate and alter our DNA for better cell regeneration.

changeling-chameleon-start = We adapt our skin to the environment.
changeling-chameleon-end = Our skin no longer blends in.

changeling-hivemind-start = We attune our brainwaves to match the greater hivemind.
changeling-hivemind-end = Our connection to the greater hivemind is severed.

changeling-mindshield-start = We shape our mental patterns to imitate mindshielding.
changeling-mindshield-end = We cease our mindshield pattern shaping.
changeling-mindshield-fail = We already have an implanted mindshield.
changeling-mindshield-overwrite = Our mindshield patterns give way to the implant.
