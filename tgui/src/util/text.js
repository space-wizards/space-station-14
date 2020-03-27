export function upperCaseFirst (str) {
  return str[0].toUpperCase() + str.slice(1).toLowerCase()
}

export function titleCase (str) {
  return str.replace(/\w\S*/g, upperCaseFirst)
}

export function zeroPad (str, pad_size) {
  str = str.toString()
  while(str.length < pad_size)
  	str = '0' + str
  return str
}
