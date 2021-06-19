# Surgeon
surgery-step-incision-success-surgeon-popup = You complete an incision in {$target}'s {$part}
surgery-step-incision-success-self-surgeon-popup = You complete an incision in your {$part}
surgery-step-incision-success-no-zone-surgeon-popup = You complete an incision in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-incision-success-target-popup = {$user} completes an incision in your {$part}

# Outsider
surgery-step-incision-success-outsider-popup = {$user} completes an incision in {$target}'s {$part}
surgery-step-incision-success-self-outsider-popup = {$user} completes an incision in {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-incision-success-no-zone-outsider-popup = {$user} completes an incision in {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
