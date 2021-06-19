# Surgeon
surgery-step-extraction-success-surgeon-popup = You extract {$target}'s {$part}
surgery-step-extraction-success-self-surgeon-popup = You extract your {$part}
surgery-step-extraction-success-no-zone-surgeon-popup = You extract {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-extraction-success-target-popup = {$user} extracts your {$part}

# Outsider
surgery-step-extraction-success-outsider-popup = {$user} extracts {$target}'s {$part}
surgery-step-extraction-success-self-outsider-popup = {$user} extracts {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-extraction-success-no-zone-outsider-popup = {$user} extracts {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
