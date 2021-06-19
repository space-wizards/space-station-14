# Surgeon
surgery-step-organmending-success-surgeon-popup = You mend {$target}'s {$part}
surgery-step-organmending-success-self-surgeon-popup = You mend your {$part}
surgery-step-organmending-success-no-zone-surgeon-popup = You mend {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-organmending-success-target-popup = {$user} mends your {$part}

# Outsider
surgery-step-organmending-success-outsider-popup = {$user} mends {$target}'s {$part}
surgery-step-organmending-success-self-outsider-popup = {$user} mends {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-organmending-success-no-zone-outsider-popup = {$user} mends {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
