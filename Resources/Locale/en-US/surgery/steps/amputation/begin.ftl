# Surgeon
surgery-step-amputation-begin-surgeon-popup = You begin to sever {$target}'s {$part}
surgery-step-amputation-begin-self-surgeon-popup = You begin to sever your {$part}
surgery-step-amputation-begin-no-zone-surgeon-popup = You begin to sever {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}

# Target
surgery-step-amputation-begin-target-popup = {$user} begins to sever your {$part}

# Outsider
surgery-step-amputation-begin-outsider-popup = {$user} begins to sever {$target}'s {$part}
surgery-step-amputation-begin-self-outsider-popup = {$user} begins to sever {GENDER($user) ->
  [male] his
  [female] her
  *[other] their
} {$part}
surgery-step-amputation-begin-no-zone-outsider-popup = {$user} begins to sever {PROPER($part) ->
  [false] the
  *[bucket] {""}
} {$part}
