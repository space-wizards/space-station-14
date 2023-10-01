mail-recipient-mismatch = Recipient name or job does not match.
mail-invalid-access = Recipient name and job match, but access isn't as expected.
mail-locked = The anti-tamper lock hasn't been removed. Tap the recipient's ID.
mail-desc-far = A parcel of mail. You can't make out who it's addressed to from this distance.
mail-desc-close = A parcel of mail addressed to {CAPITALIZE($name)}, {$job}.
mail-desc-fragile = It has a [color=red]red fragile label[/color].
mail-desc-priority = The anti-tamper lock's [color=yellow]yellow priority tape[/color] is active. Better deliver it on time!
mail-desc-priority-inactive = The anti-tamper lock's [color=#886600]yellow priority tape[/color] is inactive.
mail-unlocked = Anti-tamper system unlocked.
mail-unlocked-by-emag = Anti-tamper system *BZZT*.
mail-unlocked-reward = Anti-tamper system unlocked. {$bounty} zorkmids have been added to cargo's account.
mail-penalty-lock = ANTI-TAMPER LOCK BROKEN. CARGO BANK ACCOUNT PENALIZED BY {$credits} CREDITS.
mail-penalty-fragile = INTEGRITY COMPROMISED. CARGO BANK ACCOUNT PENALIZED BY {$credits} CREDITS.
mail-penalty-expired = DELIVERY PAST DUE. CARGO BANK ACCOUNT PENALIZED BY {$credits} CREDITS.
mail-item-name-unaddressed = mail
mail-item-name-addressed = mail ({$recipient})

command-mailto-description = Queue a parcel to be delivered to an entity. Example usage: `mailto 1234 5678 false false`. The target container's contents will be transferred to an actual mail parcel.
command-mailto-help = Usage: {$command} <recipient entityUid> <container entityUid> [is-fragile: true or false] [is-priority: true or false]
command-mailto-no-mailreceiver = Target recipient entity does not have a {$requiredComponent}.
command-mailto-no-blankmail = The {$blankMail} prototype doesn't exist. Something is very wrong. Contact a programmer.
command-mailto-bogus-mail = {$blankMail} did not have {$requiredMailComponent}. Something is very wrong. Contact a programmer.
command-mailto-invalid-container = Target container entity does not have a {$requiredContainer} container.
command-mailto-unable-to-receive = Target recipient entity was unable to be setup for receiving mail. ID may be missing.
command-mailto-no-teleporter-found = Target recipient entity was unable to be matched to any station's mail teleporter. Recipient may be off-station.
command-mailto-success = Success! Mail parcel has been queued for next teleport in {$timeToTeleport} seconds.

command-mailnow = Force all mail teleporters to deliver another round of mail as soon as possible. This will not bypass the undelivered mail limit.
command-mailnow-help = Usage: {$command}
command-mailnow-success = Success! All mail teleporters will be delivering another round of mail soon.
