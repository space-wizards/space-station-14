# Surgeon
surgery-step-retraction-begin-surgeon-popup = You begin to retract {$target}'s {$part}
surgery-step-retraction-begin-self-surgeon-popup = You begin to retract your {$part}
surgery-step-retraction-begin-no-zone-surgeon-popup = You begin to retract {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-retraction-begin-target-popup = {$user} begins to retract your {$part}

# Outsider
surgery-step-retraction-begin-outsider-popup = {$user} begins to retract {$target}'s {$part}
surgery-step-retraction-begin-self-outsider-popup = {$user} begins to retract {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-retraction-begin-no-zone-outsider-popup = {$user} begins to retract {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
