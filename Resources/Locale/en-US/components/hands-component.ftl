### HandsComponent stuff

# Examine text after when they're holding something (in-hand)
comp-hands-examine = { CAPITALIZE(SUBJECT($user)) } { CONJUGATE-BE($user) } holding a { $item }.    

# Popup text when a player uses smart equip but doesn't have a container to equip from
comp-hands-smart-equip-invalid = You have no { $equipment } to take something out of!

# Popup text when a player uses smart equip but their container is empty
comp-hands-smart-equip-empty = There's nothing in your { $equipment } to take out!

# Popup message to players who can see someone getting disarmed
comp-hands-player-witnessed-disarming = { $disarmer } disarms { $disarmed }!

# Popup message to a player when they disarm someone
comp-hands-player-disarmed-target = You disarm { $disarmed }!

# Popup message to players who can see someone getting disarmed, but had nothing in their hands to drop
comp-hands-player-witnessed-shoving = { $shover } shoves { $shoved }!

# Popup message to a player when they disarm someone, but had nothing in their hands to drop
comp-hands-player-shoved-target = You shove { $shoved }!