### HandComponent stuff

# Examine text after when they're holding something (in-hand)
comp-hands-examine = { CAPITALIZE(SUBJECT($user)) } { CONJUGATE-BE($user) } holding a { $item }.    

#Popup text when a player uses smart equip but doesn't have a container to equip from
comp-hands-smart-equip-invalid = You have no { $equipment } to take something out of!

#Popup text when a player uses smart equip but their container is empty
comp-hands-smart-equip-empty = There's nothing in your { $equipment } to take out!