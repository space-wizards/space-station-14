### HandComponent stuff

#Examine text after when they're holding something (in-hand)
comp-hands-examine = {GENDER($user) ->
    [male] He is
    [female] She is 
    *[other] They are
} holding a {$item}.    
