# Surgeon
surgery-step-drilling-begin-surgeon-popup = You begin to drill {$target}'s {$part}
surgery-step-drilling-begin-self-surgeon-popup = You begin to drill your {$part}
surgery-step-drilling-begin-no-zone-surgeon-popup = You begin to drill {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-drilling-begin-target-popup = {$user} begins to drill your {$part}

# Outsider
surgery-step-drilling-begin-outsider-popup = {$user} begins to drill {$target}'s {$part}
surgery-step-drilling-begin-self-outsider-popup = {$user} begins to drill {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-drilling-begin-no-zone-outsider-popup = {$user} begins to drill {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
