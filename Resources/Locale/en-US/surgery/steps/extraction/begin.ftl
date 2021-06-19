# Surgeon
surgery-step-extraction-begin-surgeon-popup = You begin to extract {$target}'s {$part}
surgery-step-extraction-begin-self-surgeon-popup = You begin to extract your {$part}
surgery-step-extraction-begin-no-zone-surgeon-popup = You begin to extract {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-extraction-begin-target-popup = {$user} begins to extract your {$part}

# Outsider
surgery-step-extraction-begin-outsider-popup = {$user} begins to extract {$target}'s {$part}
surgery-step-extraction-begin-self-outsider-popup = {$user} begins to extract {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-extraction-begin-no-zone-outsider-popup = {$user} begins to extract {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
