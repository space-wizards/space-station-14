import { gulp as g } from './plugins'

const out = 'assets'

import gulp from 'gulp'
export function size () {
  return gulp.src(`${out}/**`)
    .pipe(g.size())
}
