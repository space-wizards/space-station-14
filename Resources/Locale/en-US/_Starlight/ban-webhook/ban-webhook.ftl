server-ban-string-infinity = Forever
server-ban-no-name = Not found. ({ $hwid })
server-time-ban =
    Temporary ban on { $mins } { $mins ->
        [one] minute
        [few] minutes
       *[other] minutes
    }.
server-perma-ban = Permanent ban
server-role-ban =
    Temporary job-ban on { $mins } { $mins ->
        [one] minute
        [few] minutes
       *[other] minutes
    }.
server-perma-role-ban = Permanent job-ban
server-time-ban-string =
    > **Offender**
    > **Login:** ``{ $targetName }``
    > **Discord:** { $targetLink }
    
    > **Administrator**
    > **Login:** ``{ $adminName }``
    > **Discord:** { $adminLink }
    
    > **Time**
    > **Extended:** { $TimeNow }
    > **Expires:** { $expiresString }
    
    > **Reason:** { $reason }
    
    > **Severity Level:** { $severity }
server-ban-footer = { $server } | Round: #{ $round }
server-perma-ban-string =
    > **Offender**
    > **Login:** ``{ $targetName }``
    > **Discord:** { $targetLink }
    
    > **Administrator**
    > **Login:** ``{ $adminName }``
    > **Discord:** { $adminLink }
    
    > **Time**
    > **Extended:** { $TimeNow }
    
    > **Reason:** { $reason }
    
    > **Severity Level:** { $severity }
server-role-ban-string =
    > **Offender**
    > **Login:** ``{ $targetName }``
    > **Discord:** { $targetLink }
    
    > **Administrator**
    > **Login:** ``{ $adminName }``
    > **Discord:** { $adminLink }
    
    > **Time**
    > **Extended:** { $TimeNow }
    > **Expires:** { $expiresString }
    
    > **Roles:** { $roles }
    
    > **Reason:** { $reason }
    
    > **Severity Level:** { $severity }
server-perma-role-ban-string =
    > **Offender**
    > **Login:** ``{ $targetName }``
    > **Discord:** ``{ $targetLink }``
    
    > **Administrator**
    > **Login:** ``{ $adminName }``
    > **Discord:** { $adminLink }
    
    > **Time**
    > **Extended:** { $TimeNow }
    
    > **Roles:** { $roles }
    
    > **Reason:** { $reason }
    
    > **Severity Level:** { $severity }