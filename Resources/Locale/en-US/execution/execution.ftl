execution-verb-name = Execute
execution-verb-message = Use your weapon to execute someone.

# All the below localisation strings have access to the following variables
# attacker (the person committing the execution)
# victim (the person being executed)
# weapon (the weapon used for the execution)

execution-popup-gun-initial-internal = You ready the muzzle of {THE($weapon)} against {$victim}'s head.
execution-popup-gun-initial-external = {$attacker} readies the muzzle of {THE($weapon)} against {$victim}'s head.
execution-popup-gun-complete-internal = You blast {$victim} in the head!
execution-popup-gun-complete-external = {$attacker} blasts {$victim} in the head!
execution-popup-gun-clumsy-internal = You miss {$victim}'s head and shoot your foot instead!
execution-popup-gun-clumsy-external = {$attacker} misses {$victim} and shoots {POSS-ADJ($attacker)} foot instead!
execution-popup-gun-empty = {CAPITALIZE(THE($weapon))} clicks.

suicide-popup-gun-initial-internal = You place the muzzle of {THE($weapon)} in your mouth.
suicide-popup-gun-initial-external = {$attacker} places the muzzle of {THE($weapon)} in {POSS-ADJ($attacker)} mouth.
suicide-popup-gun-complete-internal = You shoot yourself in the head!
suicide-popup-gun-complete-external = {$attacker} shoots {REFLEXIVE($attacker)} in the head!

execution-popup-melee-initial-internal = You ready {THE($weapon)} against {$victim}'s throat.
execution-popup-melee-initial-external = {$attacker} readies {POSS-ADJ($attacker)} {$weapon} against the throat of {$victim}.
execution-popup-melee-complete-internal = You slit the throat of {$victim}!
execution-popup-melee-complete-external = {$attacker} slits the throat of {$victim}!

suicide-popup-melee-initial-internal = You ready {THE($weapon)} against your throat.
suicide-popup-melee-initial-external = {$attacker} readies {POSS-ADJ($attacker)} {$weapon} against {POSS-ADJ($attacker)} throat.
suicide-popup-melee-complete-internal = You slit your throat with {THE($weapon)}!
suicide-popup-melee-complete-external = {$attacker} slits {POSS-ADJ($attacker)} throat with {THE($weapon)}!