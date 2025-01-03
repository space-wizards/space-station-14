execution-verb-name = Execute
execution-verb-message = Use your weapon to execute someone.

suicide-verb-name = Suicide
suicide-verb-message = Use your weapon to suicide.

# All the below localisation strings have access to the following variables
# attacker (the person committing the execution)
# victim (the person being executed)
# weapon (the weapon used for the execution)

execution-popup-melee-initial-internal = You ready {THE($weapon)} against {THE($victim)}'s throat.
execution-popup-gun-initial-internal = You point the muzzle of {THE($weapon)} at the head of {THE($victim)}.

execution-popup-melee-initial-external = { CAPITALIZE(THE($attacker)) } readies {POSS-ADJ($attacker)} {$weapon} against the throat of {THE($victim)}.
execution-popup-gun-initial-external  = { CAPITALIZE(THE($attacker)) } points the muzzle of {POSS-ADJ($attacker)} {$weapon} at the head of {THE($victim)}.

execution-popup-melee-complete-internal = You slit the throat of {THE($victim)}!
execution-popup-gun-complete-internal = You shoot {THE($victim)} in the head!

execution-popup-melee-complete-external = { CAPITALIZE(THE($attacker)) } slits the throat of {THE($victim)}!
execution-popup-gun-complete-external = { CAPITALIZE(THE($attacker)) } shoots {THE($victim)} in the head!

execution-popup-gun-clumsy-internal = You miss the head of {THE($victim)} and shoot yourself in the foot instead!
execution-popup-gun-clumsy-external = { CAPITALIZE(THE($attacker)) } misses {THE($victim)} and shoots himself in the foot instead!

execution-popup-gun-empty = { CAPITALIZE(THE($weapon)) } clicks.

execution-popup-self-melee-initial-internal = You ready {THE($weapon)} against your own throat.
execution-popup-self-gun-initial-internal = You put the muzzle of {THE($weapon)} in your mouth.

execution-popup-self-melee-initial-external = { CAPITALIZE(THE($attacker)) } readies {POSS-ADJ($attacker)} {$weapon} against their own throat.
execution-popup-self-gun-initial-external = { CAPITALIZE(THE($attacker)) } puts the muzzle of {POSS-ADJ($attacker)} {$weapon} in his mouth.

execution-popup-self-melee-complete-internal = You slit your own throat!
execution-popup-self-gun-complete-internal = You're shooting yourself in the head!

execution-popup-self-melee-complete-external = { CAPITALIZE(THE($attacker)) } slits their own throat!
execution-popup-self-gun-complete-external = { CAPITALIZE(THE($attacker)) } shoots himself in the head!
