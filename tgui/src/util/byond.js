const encode = encodeURIComponent

// Helper to generate a BYOND href given 'params' as an object
// (with an optional 'url' for eg winset).
export const href = (url, params = {}) => {
  return `byond://${url || ''}?`
    + Object.keys(params)
      .map(key => `${encode(key)}=${encode(params[key])}`)
      .join('&');
};

// Helper to make a BYOND ui_act() call on the UI 'src' given an 'action'
// and optional 'params'.
export const act = (src, action, params = {}) => {
  window.location.href = href('', Object.assign({ src, action }, params))
};

/**
 * A high-level abstraction of BYJAX. Makes a call to BYOND and returns
 * a promise, which (if endpoint has a callback parameter) resolves
 * with the return value of that call.
 */
export const callByond = (url, params = {}) => {
  // Create a callback array if it doesn't exist yet
  window.byondCallbacks = window.byondCallbacks || [];
  // Create a Promise and push its resolve function into callback array
  const callbackIndex = window.byondCallbacks.length;
  const promise = new Promise(resolve => {
    // TODO: Fix a potential memory leak
    window.byondCallbacks.push(resolve);
  });
  // Call BYOND client
  window.location.href = href(url || '', Object.assign({}, params, {
    callback: `byondCallbacks[${callbackIndex}]`,
  }));
  // Return promise (awaitable)
  return promise;
};

export const runCommand = command => callByond('winset', { command });

/**
 * A simple debug print.
 * 
 * TODO: Find a better way to debug print.
 * Right now we just print into the game chat.
 */
export const debugPrint = (...args) => {
  const str = args
    .map(arg => {
      if (typeof arg === 'string') {
        return arg
      }
      return JSON.stringify(arg);
    })
    .join(' ');
  return runCommand('Me [debugPrint] ' + str);
};

export const winget = (win, key) => {
  return callByond('winget', { id: win, property: key })
    .then(obj => obj[key]);
};

// Helper to make a BYOND winset() call on 'window', setting 'key' to 'value'
export const winset = (win, key, value) => {
  window.location.href = href('winset', {
    [`${win}.${key}`]: value,
  });
};
