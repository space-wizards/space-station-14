# Surgeon
surgery-step-bonesaw-success-surgeon-popup = You saw through {$target}'s {$part}
surgery-step-bonesaw-success-self-surgeon-popup = You saw through your {$part}
surgery-step-bonesaw-success-no-zone-surgeon-popup = You saw through {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-bonesaw-success-target-popup = {$user} saws through your {$part}

# Outsider
surgery-step-bonesaw-success-outsider-popup = {$user} saws through {$target}'s {$part}
surgery-step-bonesaw-success-self-outsider-popup = {$user} saws through {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-bonesaw-success-no-zone-outsider-popup = {$user} saws through {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
