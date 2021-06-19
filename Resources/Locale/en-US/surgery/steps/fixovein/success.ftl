# Surgeon
surgery-step-fixovein-success-surgeon-popup = You mend the blood vessels in {$target}'s {$part}
surgery-step-fixovein-success-self-surgeon-popup = You mend the blood vessels in your {$part}
surgery-step-fixovein-success-no-zone-surgeon-popup = You mend the blood vessels in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-fixovein-success-target-popup = {$user} mends the blood vessels in your {$part}

# Outsider
surgery-step-fixovein-success-outsider-popup = {$user} mends the blood vessels in {$target}'s {$part}
surgery-step-fixovein-success-self-outsider-popup = {$user} mends the blood vessels in {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-fixovein-success-no-zone-outsider-popup = {$user} mends the blood vessels in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
