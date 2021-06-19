# Surgeon
surgery-step-bonesaw-begin-surgeon-popup = You begin to saw through {$target}'s {$part}
surgery-step-bonesaw-begin-self-surgeon-popup = You begin to saw through your {$part}
surgery-step-bonesaw-begin-no-zone-surgeon-popup = You begin to saw through {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-bonesaw-begin-target-popup = {$user} begins to saw through your {$part}

# Outsider
surgery-step-bonesaw-begin-outsider-popup = {$user} begins to saw through {$target}'s {$part}
surgery-step-bonesaw-begin-self-outsider-popup = {$user} begins to saw through {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-bonesaw-begin-no-zone-outsider-popup = {$user} begins to saw through {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
