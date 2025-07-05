subtitle-item =
    { $left ->
        [true] { $right ->
            [true] { $count ->
                [0] < {$sound} >
                [1] < {$sound} >
               *[other] < {$sound} ×{$count} >
            }
           *[false]{ $count ->
                [0] < {$sound}
                [1] < {$sound}
               *[other] < {$sound} ×{$count}
            }
        }
       *[false] { $right ->
            [true] { $count ->
                [0] {$sound} >
                [1] {$sound} >
               *[other] {$sound} ×{$count} >
            }
           *[false] { $count ->
                [0] {$sound}
                [1] {$sound}
               *[other] {$sound} ×{$count}
            }
        }
    }
