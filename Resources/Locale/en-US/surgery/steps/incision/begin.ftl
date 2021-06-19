# Surgeon
surgery-step-incision-begin-surgeon-popup = You begin an incision in {$target}'s {$part}
surgery-step-incision-begin-self-surgeon-popup = You begin an incision in your {$part}
surgery-step-incision-begin-no-zone-surgeon-popup = You begin an incision in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-incision-begin-target-popup = {$user} begins an incision in your {$part}

# Outsider
surgery-step-incision-begin-outsider-popup = {$user} begins an incision in {$target}'s {$part}
surgery-step-incision-begin-self-outsider-popup = {$user} begins an incision in {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-incision-begin-no-zone-outsider-popup = {$user} begins an incision in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
