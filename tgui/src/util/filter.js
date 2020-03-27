export function filterMulti (displays, string) {
  for (let display of displays) { // First check if the display includes the search term in the first place.
    if (display.textContent.toLowerCase().includes(string)) {
      display.style.display = ''
      filter(display, string)
    } else {
      display.style.display = 'none'
    }
  }
}

export function filter (display, string) {
  const items = display.queryAll('section')
  const titleMatch = display.query('header').textContent.toLowerCase().includes(string)
  for (let item of items) { // Check if the item or its displays title contains the search term.
    if (titleMatch || item.textContent.toLowerCase().includes(string)) {
      item.style.display = ''
    } else {
      item.style.display = 'none'
    }
  }
}
