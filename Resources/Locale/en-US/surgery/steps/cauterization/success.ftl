# Surgeon
surgery-step-cauterization-success-surgeon-popup = You cauterize {$target}'s {$part}
surgery-step-cauterization-success-self-surgeon-popup = You cauterize your {$part}
surgery-step-cauterization-success-no-zone-surgeon-popup = You cauterize {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-cauterization-success-target-popup = {$user} cauterizes your {$part}

# Outsider
surgery-step-cauterization-success-outsider-popup = {$user} cauterizes {$target}'s {$part}
surgery-step-cauterization-success-self-outsider-popup = {$user} cauterizes {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-cauterization-success-no-zone-outsider-popup = {$user} cauterizes {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
