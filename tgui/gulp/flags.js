const flags = require('minimist')(process.argv.slice(2))

export const src = flags.src || 'src'
export const dest = flags.dest || 'assets'

export const debug = flags.debug || flags.d
export const min = flags.min || flags.m
