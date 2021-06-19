# Surgeon
surgery-step-bonesetter-success-surgeon-popup = You set {$target}'s {$part}
surgery-step-bonesetter-success-self-surgeon-popup = You set your {$part}
surgery-step-bonesetter-success-no-zone-surgeon-popup = You set {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-bonesetter-success-target-popup = {$user} sets your {$part}

# Outsider
surgery-step-bonesetter-success-outsider-popup = {$user} sets {$target}'s {$part}
surgery-step-bonesetter-success-self-outsider-popup = {$user} sets {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-bonesetter-success-no-zone-outsider-popup = {$user} sets {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
