# Surgeon
surgery-step-cauterization-begin-surgeon-popup = You begin to cauterize {$target}'s {$part}
surgery-step-cauterization-begin-self-surgeon-popup = You begin to cauterize your {$part}
surgery-step-cauterization-begin-no-zone-surgeon-popup = You begin to cauterize {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-cauterization-begin-target-popup = {$user} begins to cauterize your {$part}

# Outsider
surgery-step-cauterization-begin-outsider-popup = {$user} begins to cauterize {$target}'s {$part}
surgery-step-cauterization-begin-self-outsider-popup = {$user} begins to cauterize {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-cauterization-begin-no-zone-outsider-popup = {$user} begins to cauterize {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
