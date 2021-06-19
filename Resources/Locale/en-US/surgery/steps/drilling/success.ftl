# Surgeon
surgery-step-drilling-success-surgeon-popup = You drill {$target}'s {$part}
surgery-step-drilling-success-self-surgeon-popup = You drill your {$part}
surgery-step-drilling-success-no-zone-surgeon-popup = You drill {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-drilling-success-target-popup = {$user} drills your {$part}

# Outsider
surgery-step-drilling-success-outsider-popup = {$user} drills {$target}'s {$part}
surgery-step-drilling-success-self-outsider-popup = {$user} drills {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-drilling-success-no-zone-outsider-popup = {$user} drills {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
