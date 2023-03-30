## Steps
# there is no self-no-zone variant so you shouldn't go making handed grapes doing surgeries on themselves
# but you wouldn't anyway, right?

# Begin
surgery-step-begin-popup = {$user} begins {$action} {$target}'s {$part}
surgery-step-begin-self-popup = {$user} begins {$action} {POSS-ADJ($target)} {$part}
surgery-step-begin-no-zone-popup = {$user} begins {$action} the {THE($part)}

# Success
surgery-step-success-popup = {$user} {$action} {$target}'s {$part}
surgery-step-success-self-popup = {$user} {$action} {POSS-ADJ($target)} {$part}
surgery-step-success-no-zone-popup = {$user} {$action} the {THE($part)}

surgery-step-not-useful = You see no useful way to do that.
surgery-step-no-organ-selected = You need to select an organ to extract first.

surgery-insert-success-popup = {$user} inserts {$item} into {$target}'s {$part}
surgery-insert-success-self-popup = {$user} inserts {$item} into {POSS-ADJ($user)} {$part}
surgery-insert-success-no-zone-popup = {$user} inserts {$item} into the {THE($part)}
surgery-insert-success-self-no-zone-popup = {$user} inserts {$item} into {REFLEXIVE($user)}

surgery-aborted = {$user} cauterizes {$target}'s botched surgery!

### Windows
ui-surgery-operations-window-title = Operations
ui-surgery-organs-window-title = Organs
