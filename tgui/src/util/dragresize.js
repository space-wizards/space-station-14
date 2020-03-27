import {winset} from './byond'

export function lock (x, y) {
  if (x < 0) { // Left
    x = 0
  } else if (x + window.innerWidth > window.screen.availWidth) { // Right
    x = window.screen.availWidth - window.innerWidth
  }

  if (y < 0) { // Top
    y = 0
  } else if (y + window.innerHeight > window.screen.availHeight) { // Bottom
    y = window.screen.availHeight - window.innerHeight
  }

  return {x, y}
}

export function drag (event) {
  event.preventDefault();
  if (!this.get('drag')) {
    return;
  }
  if (this.get('x')) {
    let x = event.screenX
      + this.get('x')
      + this.get('screenOffsetX');
    let y = event.screenY
      + this.get('y')
      + this.get('screenOffsetY');
    winset(this.get('config.window'), 'pos', `${x},${y}`);
  }
  else {
    this.set({
      x: window.screenLeft - event.screenX,
      y: window.screenTop - event.screenY,
    });
  }
}

export function sane (x, y) {
  x = Math.clamp(100, window.screen.width, x)
  y = Math.clamp(100, window.screen.height, y)
  return {x, y}
}

export function resize (event) {
  event.preventDefault()

  if (!this.get('resize')) return

  if (this.get('x')) {
    let x = (event.screenX - this.get('x')) + window.innerWidth
    let y = (event.screenY - this.get('y')) + window.innerHeight
    ;({x, y} = sane(x, y))
    winset(this.get('config.window'), 'size', `${x},${y}`)
  }
  this.set({ x: event.screenX, y: event.screenY })
}
