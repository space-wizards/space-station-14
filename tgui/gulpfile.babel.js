import gulp from 'gulp'

import { css, watch_css } from './gulp/css'
import { js, watch_js } from './gulp/js'
import { reload, watch_reload } from './gulp/reload'
import { size } from './gulp/size'

gulp.task(reload)
gulp.task(size)

gulp.task('default', gulp.series(gulp.parallel(css, js), size))
gulp.task('watch', gulp.parallel(watch_css, watch_js, watch_reload))
