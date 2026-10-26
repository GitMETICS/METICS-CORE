"""Inventario estático de rutas; no ejecuta acciones ni accede a la base de datos."""
import collections
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parent.parent
OUT = pathlib.Path(__file__).resolve().parent


def mask(source):
    pattern = r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\''
    return re.sub(pattern, lambda m: re.sub(r'[^\n]', ' ', m[0]), source)


def closing(source, start, left, right):
    depth = 0
    for pos in range(start, len(source)):
        if source[pos] == left:
            depth += 1
        elif source[pos] == right:
            depth -= 1
            if depth == 0:
                return pos
    raise ValueError(start)


def strip_view_comments(source):
    return re.sub(r'@\*[\s\S]*?\*@|<!--[\s\S]*?-->', lambda m: '\n' * m[0].count('\n'), source)


items = []
for path in sorted((ROOT / 'webMetics/Controllers').glob('*Controller.cs')):
    source = path.read_text(encoding='utf-8-sig')
    clean = mask(source)
    controller = path.stem.removesuffix('Controller')
    previous = 0
    pattern = r'^\s*public\s+(?P<mods>(?:(?:async|static|override|virtual|new)\s+)*)(?P<ret>[^\r\n(]+?)\s+(?P<name>\w+)\s*\('
    for match in re.finditer(pattern, clean, re.M):
        opening = clean.index('(', match.start())
        ending = closing(clean, opening, '(', ')')
        body_start = clean.find('{', ending)
        body_end = closing(clean, body_start, '{', '}')
        prefix = source[previous:match.start()]
        previous = body_end + 1
        if 'static' in match['mods']:
            continue
        attrs = re.findall(r'^\s*\[(\w+)([^\r\n]*)', prefix, re.M)
        if any(a[0] == 'NonAction' for a in attrs):
            continue
        body = source[body_start:body_end + 1]
        name = match['name']
        views = []
        for view_match in re.finditer(r'return\s+(View|PartialView)\s*\(([^;]*)\)', body):
            explicit = re.match(r'"([^"]+)"', view_match[2].strip())
            view = explicit[1] if explicit else name
            candidates = [ROOT / 'webMetics/Views' / controller / (view + '.cshtml'), ROOT / 'webMetics/Views/Shared' / (view + '.cshtml')]
            views.append(dict(name=view, partial=view_match[1] == 'PartialView', exists=any(p.exists() for p in candidates)))
        verb = next((a[0][4:].upper() for a in attrs if a[0].startswith('Http')), 'ANY')
        kind = 'Página' if any(not v['partial'] for v in views) else 'Parcial' if views else 'JSON' if re.search(r'\bJson\(', body) else 'Archivo' if re.search(r'\bFile\(', body) else 'Operación/redirección'
        items.append(dict(controller=controller, action=name, verb=verb, params=' '.join(source[opening + 1:ending].split()), returns=match['ret'].strip(), line=source.count('\n', 0, clean.index('public', match.start())) + 1, file=path.relative_to(ROOT).as_posix(), kind=kind, views=views, redirects=list(dict.fromkeys(re.findall(r'RedirectToAction\(([^\r\n;]+)', body)))))

edges = []
dynamic = []
for path in sorted((ROOT / 'webMetics').rglob('*')):
    if path.suffix not in ('.cshtml', '.js') or any(p in path.parts for p in ('bin', 'obj', 'lib')):
        continue
    source = strip_view_comments(path.read_text(encoding='utf-8-sig', errors='replace'))
    default = path.parent.name if path.suffix == '.cshtml' else None
    patterns = [
        (r'Html\.ActionLink\(\s*"([^"]*)"\s*,\s*"([^"]+)"\s*(?:,\s*"([^"]+)")?', 'actionlink'),
        (r'Url\.Action\(\s*"([^"]+)"\s*(?:,\s*"([^"]+)")?', 'url'),
        (r'Html\.BeginForm\(\s*"([^"]+)"\s*,\s*"([^"]+)"', 'formhelper'),
        (r'<form\b[^>]*>', 'form'),
        (r'[\'"`]~?/(\w+)/(\w+)', 'literal'),
    ]
    for pattern, kind in patterns:
        for match in re.finditer(pattern, source):
            action = controller = None
            if kind == 'actionlink':
                action, controller = match[2], match[3] or default
            elif kind in ('url', 'formhelper'):
                action, controller = match[1], match[2] or default
            elif kind == 'form':
                a = re.search(r'asp-action="([^"]+)"', match[0])
                c = re.search(r'asp-controller="([^"]+)"', match[0])
                action, controller = a[1] if a else None, c[1] if c else default
            else:
                controller, action = match[1], match[2]
                if controller.lower() in ('images', 'css', 'js', 'lib', 'views'):
                    continue
            if action and controller:
                line = source.count('\n', 0, match.start()) + 1
                edges.append(dict(controller=controller, action=action, file=path.relative_to(ROOT).as_posix(), line=line))
    for match in re.finditer(r'Url\.Action\(\s*[^"\s][^\r\n]*', source):
        dynamic.append(dict(file=path.relative_to(ROOT).as_posix(), line=source.count('\n', 0, match.start()) + 1, expression=match[0]))

for item in items:
    item['incoming'] = list(dict.fromkeys(e['file'] for e in edges if (e['controller'].lower(), e['action'].lower()) == (item['controller'].lower(), item['action'].lower())))

inventory = dict(items=items, edges=edges, dynamic=dynamic)
(OUT / 'inventory.json').write_text(json.dumps(inventory, ensure_ascii=False, indent=2), encoding='utf-8')
known = {(i['controller'].lower(), i['action'].lower()) for i in items}
missing = collections.defaultdict(list)
for edge in edges:
    if (edge['controller'].lower(), edge['action'].lower()) not in known:
        missing[(edge['controller'], edge['action'])].append(edge)
print(f"{len(items)} métodos públicos de instancia; {len(known)} paths distintos; {len(set(i['controller'] for i in items))} controladores")
print('REFERENCIAS SIN ACCIÓN COINCIDENTE (revisión manual requerida):')
for key, refs in missing.items():
    print('/' + '/'.join(key))
    for edge in refs[:4]:
        print(' ', edge['file'], edge['line'])
print('PÁGINAS GET/ANY:')
for item in items:
    if item['kind'] == 'Página' and item['verb'] != 'POST':
        print(item['controller'] + '/' + item['action'], '(' + item['params'] + ')', 'vistas=' + ','.join(v['name'] + (' FALTA' if not v['exists'] else '') for v in item['views']))

report_path = OUT / 'MAPA_RUTAS_BREADCRUMBS.md'
if report_path.exists():
    marker = '<!-- INVENTARIO_GENERADO -->'
    introduction = report_path.read_text(encoding='utf-8').split(marker)[0].rstrip()
    lines = [introduction, '', marker, '']
    for controller in sorted(set(i['controller'] for i in items)):
        group = [i for i in items if i['controller'] == controller]
        lines += [f'### {controller} ({len(group)} métodos)', '', '| Método | Path | Parámetros | Respuesta/vista | Fuente |', '|---|---|---|---|---|']
        for item in group:
            views = ', '.join(dict.fromkeys(v['name'] + (' **VISTA AUSENTE**' if not v['exists'] else '') for v in item['views']))
            response = item['kind'] + (': ' + views if views else '')
            if item['action'] in ('PuedeEliminarAsesor', 'ConstructorDelMensajeNotificacionInscripcion', 'FetchCookieValue'):
                response = 'Auxiliar público; no es una pantalla'
            params = '`' + item['params'].replace('|', '\\|') + '`' if item['params'] else '—'
            lines.append(f"| {item['verb']} | `/{controller}/{item['action']}` | {params} | {response} | `{item['file']}:{item['line']}` |")
        lines.append('')
    lines += ['## 9. Aliases de POST que devuelven formularios', '', 'Estos métodos pueden mostrar una vista completa al fallar una validación. El breadcrumb debe describir el formulario/resultante y nunca ofrecer un enlace GET a la acción de guardado.', '', '| POST | Vista devuelta | Pantalla/criterio para breadcrumb |', '|---|---|---|']
    canonical = {}
    for item in items:
        if item['verb'] == 'POST':
            continue
        for view in item['views']:
            if view['exists'] and item['action'] == view['name']:
                canonical[(item['controller'], view['name'])] = '/' + item['controller'] + '/' + item['action']
    for item in items:
        if item['verb'] != 'POST' or item['kind'] != 'Página':
            continue
        for name in dict.fromkeys(v['name'] for v in item['views']):
            target = canonical.get((item['controller'], name))
            display = '`' + target + '` (conservar parámetros del modelo)' if target else 'Vista de resultado sin ruta GET canónica identificada; último elemento no clicable'
            lines.append(f"| `/{item['controller']}/{item['action']}` | `{name}` | {display} |")
    lines += ['', '## 10. Referencias dinámicas que requieren resolver contexto', '', 'Se encontraron estas expresiones de destino variable; la revisión manual debe resolver sus valores y los parámetros antes de generar migas.', '']
    for item in dynamic:
        lines.append(f"- `{item['file']}:{item['line']}`: `{item['expression'].replace('`', '')}`")
    report_path.write_text('\n'.join(lines) + '\n', encoding='utf-8')
