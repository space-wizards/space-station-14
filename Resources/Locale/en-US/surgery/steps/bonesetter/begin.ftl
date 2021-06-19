# Surgeon
surgery-step-bonesetter-begin-surgeon-popup = You begin to set {$target}'s {$part}
surgery-step-bonesetter-begin-self-surgeon-popup = You begin to set your {$part}
surgery-step-bonesetter-begin-no-zone-surgeon-popup = You begin to set {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-bonesetter-begin-target-popup = {$user} begins to set your {$part}

# Outsider
surgery-step-bonesetter-begin-outsider-popup = {$user} begins to set {$target}'s {$part}
surgery-step-bonesetter-begin-self-outsider-popup = {$user} begins to set {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-bonesetter-begin-no-zone-outsider-popup = {$user} begins to set {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
