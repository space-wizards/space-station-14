# Surgeon
surgery-step-limbattachment-success-surgeon-popup = You attach {$target}'s {$part}
surgery-step-limbattachment-success-self-surgeon-popup = You attach your {$part}
surgery-step-limbattachment-success-no-zone-surgeon-popup = You attach {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-limbattachment-success-target-popup = {$user} attaches your {$part}

# Outsider
surgery-step-limbattachment-success-outsider-popup = {$user} attaches {$target}'s {$part}
surgery-step-limbattachment-success-self-outsider-popup = {$user} attaches {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-limbattachment-success-no-zone-outsider-popup = {$user} attaches {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
