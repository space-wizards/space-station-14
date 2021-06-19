# Surgeon
surgery-step-organmending-begin-surgeon-popup = You begin to mend {$target}'s {$part}
surgery-step-organmending-begin-self-surgeon-popup = You begin to mend your {$part}
surgery-step-organmending-begin-no-zone-surgeon-popup = You begin to mend {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-organmending-begin-target-popup = {$user} begins to mend your {$part}

# Outsider
surgery-step-organmending-begin-outsider-popup = {$user} begins to mend {$target}'s {$part}
surgery-step-organmending-begin-self-outsider-popup = {$user} begins to mend {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-organmending-begin-no-zone-outsider-popup = {$user} begins to mend {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
