### UI

# Displayed in the Character prefs window
humanoid-character-profile-summary =
    This is {$name}. {$gender ->
    [male] He is
    [female] She is
    [epicene] They are
    [neuter] It is
    *[other] {$gender}
} {$conjugate-be ->
    [male] is
    [female] is
    [neuter] is
    [false] is
    *[other] are
} {$age} years old.
