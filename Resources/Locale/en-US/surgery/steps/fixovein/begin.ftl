# Surgeon
surgery-step-fixovein-begin-surgeon-popup = You begin to mend the blood vessels in {$target}'s {$part}
surgery-step-fixovein-begin-self-surgeon-popup = You begin to mend the blood vessels in your {$part}
surgery-step-fixovein-begin-no-zone-surgeon-popup = You begin to mend the blood vessels in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-fixovein-begin-target-popup = {$user} begins to mend the blood vessels in your {$part}

# Outsider
surgery-step-fixovein-begin-outsider-popup = {$user} begins to mend the blood vessels in {$target}'s {$part}
surgery-step-fixovein-begin-self-outsider-popup = {$user} begins to mend the blood vessels in {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-fixovein-begin-no-zone-outsider-popup = {$user} begins to mend the blood vessels in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
