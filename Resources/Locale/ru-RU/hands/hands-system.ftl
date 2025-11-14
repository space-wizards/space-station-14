## HandsSystem

# Examine text after when they're holding something (in-hand)
comp-hands-examine = { CAPITALIZE(SUBJECT($user)) } держит { $items }.
comp-hands-examine-empty = { CAPITALIZE(SUBJECT($user)) } ничего не держит.
comp-hands-examine-wrapper = [color=paleturquoise]{ $item }[/color]
hands-system-blocked-by = Руки заняты
