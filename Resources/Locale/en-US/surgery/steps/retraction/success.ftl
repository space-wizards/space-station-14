# Surgeon
surgery-step-retraction-success-surgeon-popup = You retract {$target}'s {$part}
surgery-step-retraction-success-self-surgeon-popup = You retract your {$part}
surgery-step-retraction-success-no-zone-surgeon-popup = You retract {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-retraction-success-target-popup = {$user} retracts your {$part}

# Outsider
surgery-step-retraction-success-outsider-popup = {$user} retracts {$target}'s {$part}
surgery-step-retraction-success-self-outsider-popup = {$user} retracts {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-retraction-success-no-zone-outsider-popup = {$user} retracts {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
