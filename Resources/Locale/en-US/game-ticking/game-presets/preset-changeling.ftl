## CHANGELINGS

ling-round-end-name = changeling

objective-issuer-changeling = [color=red]Hivemind[/color]

changelings-title = Changelings
changelings-description = placeholder dumbass
changelings-not-enough-ready-players = Not enough players readied up for the game! There were {$readyPlayersCount} players readied up out of {$minimumPlayers} needed. Can't start Changelings.
changelings-no-one-ready = No players readied up! Can't start Changelings.

# Ling role
changeling-role-greeting =
    You are a changeling who has absorbed and taken the form of {$character-name}.
    Your objectives are listed in the character menu, use your special abilities to help complete them.

changeling-role-greeting-short =
    You are a changeling who has absorbed 
    and taken the form of {$character-name}.
    Use your special abilities to help complete your objectives.


# Ling abilities
changeling-not-enough-chemicals = Not enough chemicals!

changeling-armblade-fail = You have no free hands!
changeling-armblade-success-others = A grotesque blade forms around {THE($user)}'s arm!
changeling-armblade-success-self = Your arm twists and mutates, transforming it into a deadly blade.
changeling-armblade-retract-others = {CAPITALIZE(THE($target))}'s arm blade reforms into an arm!
changeling-armblade-retract-self = You assimilate the arm blade back into your body.

changeling-armor-success-others = {CAPITALIZE(THE($target))}'s body inflates into a mass of armor!
changeling-armor-success-self = Your body inflates into a mass of armor.
changeling-armor-retract-others = {CAPITALIZE(THE($target))}'s armor bursts off! 
changeling-armor-retract-self = Your armor bursts off.

changeling-regenerate-others-success = {CAPITALIZE(THE($user))} rapidly regenerates their body, making a loud, grotesque sound!
changeling-regenerate-self-success = You feel an itching, both inside and outside as your tissues knit and reknit!
changeling-regenerate-fail-not-crit = You aren't in a critical condition!
changeling-regenerate-fail-dead = You're dead!

changeling-chameleon-toggle-on = Your skin shimmers with transparency...
changeling-chameleon-toggle-off = Your skin reverts back to normal.

changeling-dna-stage-1 = This creature is compatible. You must hold still...
changeling-dna-stage-2-self = You extend a proboscis.
changeling-dna-stage-2-others = {CAPITALIZE(THE($user))} extends a proboscis!
changeling-dna-stage-3-self = You stab {THE($target)} with the proboscis.
changeling-dna-stage-3-others = {CAPITALIZE(THE($user))} stabs {THE($target)} with the proboscis!
changeling-dna-success = You have absorbed {THE($target)}.
changeling-dna-success-ling = You have absorbed {THE($target)}. They were another changeling! You have gained 5 evolution points.
changeling-dna-fail-nohuman = {CAPITALIZE(THE($target))} isn't a humanoid.
changeling-dna-fail-notdead = {CAPITALIZE(THE($target))} isn't dead or in critical condition.
changeling-dna-interrupted = You were interrupted while absorbing {THE($target)}.
changeling-dna-alreadyabsorbed = {CAPITALIZE(THE($target))}'s DNA has been absorbed already!
changeling-dna-nodna = {CAPITALIZE(THE($target))} does not have DNA!
changeling-dna-switchdna = Switched to {$target}'s DNA.

changeling-transform-activate = You transform into {$target}.
changeling-transform-fail = You're already morphed as {$target}!

changeling-sting-fail-self = The sting was ineffective on {THE($target)}!
changeling-sting-fail-target = You feel a slight sting.

changeling-dna-sting = You extract the DNA of {THE($target)}.
changeling-dna-sting-fail-nodna = {CAPITALIZE(THE($target))} has no DNA!
changeling-dna-sting-fail-alreadydna = You already have {THE($target)}'s DNA!