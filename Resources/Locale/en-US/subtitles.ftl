subtitle-item =
    { $left ->
        [true] { $right ->
            [true] { $count ->
                [1] < {$sound} >
               *[other] < {$sound} ×{$count} >
            }
           *[false]{ $count ->
                [1] < {$sound}
               *[other] < {$sound} ×{$count}
            }
        }
       *[false] { $right ->
            [true] { $count ->
                [1] {$sound} >
               *[other] {$sound} ×{$count} >
            }
           *[false] { $count ->
                [1] {$sound}
               *[other] {$sound} ×{$count}
            }
        }
    }
