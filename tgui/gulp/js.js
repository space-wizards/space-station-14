import * as f from './flags'
import { browserify as b, gulp as g } from './plugins'

const entry = 'tgui.js'

import { transform as babel } from 'babel-core'
import { readFileSync as read } from 'fs'
b.componentify.compilers['text/javascript'] = function (source, file) {
  const config = { sourceMaps: true }
  Object.assign(config, JSON.parse(read(`${f.src}/.babelrc`, 'utf8')))
  const compiled = babel(source, config)

  return { source: compiled.code, map: compiled.map }
}
import { render as stylus } from 'stylus'
b.componentify.compilers['text/stylus'] = function (source, file) {
  const config = { filename: file }
  const compiled = stylus(source, config)

  return { source: compiled }
}

import browserify from 'browserify'
const bundle = browserify(`${f.src}/${entry}`, {
  debug: f.debug,
  cache: {},
  packageCache: {},
  extensions: [ '.js', '.ract' ],
  paths: [ f.src ]
})
if (f.min) bundle.plugin(b.collapse)
bundle
  .transform(b.babelify)
  .plugin(b.helpers)
  .transform(b.componentify)
  .transform(b.globify)
  .transform(b.es3ify, { global: true })

import buffer from 'vinyl-buffer'
import gulp from 'gulp'
import source from 'vinyl-source-stream'
export function js () {
  return bundle.bundle()
    .pipe(source(entry))
    .pipe(buffer())
    .pipe(g.if(f.debug, g.sourcemaps.init({loadMaps: true})))
    .pipe(g.bytediff.start())
    .pipe(g.if(f.min, g.uglify({
      mangle: true,
      compress: {
        unsafe: false,
      },
      ie8: true,
    })))
    .pipe(g.if(f.debug, g.sourcemaps.write()))
    .pipe(g.bytediff.stop())
    .pipe(gulp.dest(f.dest))
}
import gulplog from 'gulplog'
export function watch_js () {
  bundle.plugin(b.watchify)
  bundle.on('update', js)
  bundle.on('error', err => {
    gulplog.error(err.toString())
    this.emit('end')
  })
  return js()
}
