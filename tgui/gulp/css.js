import * as f from './flags'
import { gulp as g, postcss as s } from './plugins'

const entry = 'tgui.styl'

import gulp from 'gulp'
export function css () {
  return gulp.src(`${f.src}/${entry}`)
    .pipe(g.if(f.debug, g.sourcemaps.init({loadMaps: true})))
    .pipe(g.stylus({
      url: 'data-url',
      paths: [ f.src ]
    }))
    .pipe(g.postcss([
      s.autoprefixer({ browsers: ['last 2 versions', 'ie >= 8'] }),
      s.gradient,
      s.opacity,
      s.rgba({oldie: true}),
      s.plsfilters({oldIE: true}),
      s.fontweights,
      ... f.min ? [s.cssnano()] : [],
    ]))
    .pipe(g.bytediff.start())
    .pipe(g.if(f.debug, g.sourcemaps.write()))
    .pipe(g.bytediff.stop())
    .pipe(gulp.dest(f.dest))
}
export function watch_css () {
  gulp.watch(`${f.src}/**/*.styl`, css)
  return css()
}
