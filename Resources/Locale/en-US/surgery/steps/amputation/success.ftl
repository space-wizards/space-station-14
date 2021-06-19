# Surgeon
surgery-step-amputation-success-surgeon-popup = You sever {$target}'s {$part}
surgery-step-amputation-success-self-surgeon-popup = You sever your {$part}
surgery-step-amputation-success-no-zone-surgeon-popup = You sever {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-amputation-success-target-popup = {$user} severs your {$part}

# Outsider
surgery-step-amputation-success-outsider-popup = {$user} severs {$target}'s {$part}
surgery-step-amputation-success-self-outsider-popup = {$user} severs {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-amputation-success-no-zone-outsider-popup = {$user} severs {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
