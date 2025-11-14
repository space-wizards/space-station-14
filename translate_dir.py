import pathlib, re, sys, urllib.parse, urllib.request, json, time

def translate_chunk(text):
    base = 'https://translate.googleapis.com/translate_a/single'
    params = {
        'client': 'gtx',
        'sl': 'en',
        'tl': 'ru',
        'dt': 't',
        'q': text,
    }
    url = base + '?' + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    for attempt in range(3):
        try:
            with urllib.request.urlopen(req, timeout=20) as resp:
                data = resp.read().decode('utf-8')
            res = json.loads(data)
            return ''.join(item[0] for item in res[0])
        except Exception:
            if attempt == 2:
                raise
            time.sleep(1)
    return text

if len(sys.argv) < 3:
    print('Usage: translate_dir.py <src_dir> <dst_dir> [start] [end]')
    sys.exit(1)

src_root = pathlib.Path(sys.argv[1])
dst_root = pathlib.Path(sys.argv[2])
dst_root.mkdir(parents=True, exist_ok=True)
files = sorted(src_root.glob('*.xml'))
start = int(sys.argv[3]) if len(sys.argv) > 3 else 0
end = int(sys.argv[4]) if len(sys.argv) > 4 else len(files)
cache = {}
for src in files[start:end]:
    data = src.read_text()
    parts = re.split(r'(<[^>]+>|\[[^\]]+\])', data)
    out_parts = []
    for part in parts:
        if not part:
            continue
        if part.startswith('<') or part.startswith('['):
            out_parts.append(part)
            continue
        start_ws = 0
        end_ws = len(part)
        while start_ws < end_ws and part[start_ws].isspace():
            start_ws += 1
        while end_ws > start_ws and part[end_ws-1].isspace():
            end_ws -= 1
        if start_ws >= end_ws:
            out_parts.append(part)
            continue
        core = part[start_ws:end_ws]
        if not core.strip():
            out_parts.append(part)
            continue
        if core in cache:
            translated = cache[core]
        else:
            translated = translate_chunk(core)
            cache[core] = translated
        out_parts.append(part[:start_ws] + translated + part[end_ws:])
    dst = dst_root / src.name
    dst.write_text(''.join(out_parts))
