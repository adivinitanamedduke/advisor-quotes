#!/usr/bin/env node
// Local trace viewer for the API's text output (Observability__LogFormat=text + OpenTelemetry console exporter).
//   node scripts/trace-view.mjs api.log               list the most recent requests and their trace ids
//   node scripts/trace-view.mjs api.log <trace-id>    open an HTML view: matching log lines + span waterfall
// No dependencies. Reads UTF-8 or UTF-16 (PowerShell's Tee-Object writes UTF-16).
import { execFile } from 'node:child_process'
import { readFileSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'

const [file, traceId] = process.argv.slice(2)
if (!file) {
  console.error('Usage: node scripts/trace-view.mjs <api.log> [trace-id]')
  process.exit(1)
}

function readText(path) {
  const buf = readFileSync(path)
  if (buf[0] === 0xff && buf[1] === 0xfe) return buf.subarray(2).toString('utf16le')
  if (buf[0] === 0xef && buf[1] === 0xbb && buf[2] === 0xbf) return buf.subarray(3).toString('utf8')
  return buf.toString('utf8')
}

const lines = readText(file).split(/\r?\n/)
const logLine = /^\[(\d\d:\d\d:\d\d) (\w{3})\] ([0-9a-f]{32}) /

if (!traceId) {
  const requests = lines.filter((l) => logLine.test(l) && l.includes('RequestLoggingMiddleware'))
  if (!requests.length) console.log('No request log lines found. Run the API with Observability__LogFormat=text.')
  for (const l of requests.slice(-20)) {
    const [, time, , id] = l.match(logLine)
    console.log(`${time}  ${id}  ${l.slice(l.indexOf('HTTP ')).replace(/ in [\d.]+ ms$/, '')}`)
  }
  process.exit(0)
}

const noisyTags = new Set([
  'server.address', 'server.port', 'network.protocol.version', 'url.scheme', 'user_agent.original',
  'db.system.name', 'db.namespace', 'db.npgsql.data_source', 'db.npgsql.connection_id',
])

const logs = lines.filter((l) => logLine.test(l) && l.includes(traceId))
const spans = []
for (let i = 0; i < lines.length; i++) {
  if (!lines[i].startsWith('Activity.TraceId:') || !lines[i].includes(traceId)) continue
  const span = { tags: [] }
  let section = ''
  for (let j = i + 1; j < lines.length && !lines[j].startsWith('Activity.TraceId:') && !logLine.test(lines[j]); j++) {
    const line = lines[j]
    const field = (name) => (line.startsWith(name) ? line.slice(name.length).trim() : undefined)
    span.spanId ??= field('Activity.SpanId:')
    span.parentId ??= field('Activity.ParentSpanId:')
    span.name ??= field('Activity.DisplayName:')
    span.kind ??= field('Activity.Kind:')
    span.status ??= field('StatusCode:')
    const start = field('Activity.StartTime:')
    if (start) span.start = Date.parse(start)
    const duration = field('Activity.Duration:')
    if (duration) {
      const [h, m, s] = duration.split(':').map(Number)
      span.durationMs = ((h * 60 + m) * 60 + s) * 1000
    }
    if (line.startsWith('Activity.Tags:')) section = 'tags'
    else if (line.startsWith('Activity.')) section = ''
    else if (line.startsWith('Instrumentation scope')) section = 'scope'
    else if (line.startsWith('Resource associated')) section = ''
    else if (section === 'tags' && /^ {4}[\w.]+: /.test(line)) {
      const [k, ...v] = line.trim().split(': ')
      if (!noisyTags.has(k)) span.tags.push([k, v.join(': ')])
    } else if (section === 'tags' && span.tags.length) {
      // Multi-line SQL: fold continuation lines into the last tag
      span.tags[span.tags.length - 1][1] += ` ${line.trim()}`
    } else if (section === 'scope' && line.trim().startsWith('Name:')) span.source ??= line.split('Name:')[1].trim()
  }
  if (span.spanId && span.start !== undefined) spans.push(span)
}

if (!spans.length && !logs.length) {
  console.error(`Trace ${traceId} not found in ${file}. Spans are flushed every few seconds; try again shortly.`)
  process.exit(1)
}

const escape = (s) => String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c])
const short = (v) =>
  v.length <= 90 ? v : v.includes('UPDATE quotes SET') ? `${v.slice(0, 45)} … ${v.slice(v.indexOf('UPDATE quotes SET'))}` : `${v.slice(0, 90)}…`

const t0 = Math.min(...spans.map((s) => s.start))
const total = Math.max(1, ...spans.map((s) => s.start - t0 + s.durationMs))
const ids = new Set(spans.map((s) => s.spanId))
const children = new Map()
for (const s of spans) {
  const parent = s.parentId && ids.has(s.parentId) ? s.parentId : 'root'
  children.set(parent, [...(children.get(parent) ?? []), s])
}
const rows = []
const walk = (parent, depth) => {
  for (const s of (children.get(parent) ?? []).sort((a, b) => a.start - b.start)) {
    const cls = s.source === 'AdvisorQuotes' ? 'app' : s.source === 'Npgsql' ? 'db' : 'http'
    const left = ((s.start - t0) / total) * 100
    const width = Math.max((s.durationMs / total) * 100, 0.4)
    const tags = s.tags.map(([k, v]) => `<b>${escape(k)}</b>=${escape(short(v))}`).join('  ')
    const error = s.status === 'Error' ? ' <span class="err">ERROR</span>' : ''
    rows.push(`<tr title="${escape(s.tags.map(([k, v]) => `${k}=${v}`).join('\n'))}">
      <td class="name ${cls}" style="padding-left:${8 + depth * 18}px">${escape(s.name)}${error}<div class="src">${escape(s.source ?? '')} · ${escape(s.kind ?? '')}</div></td>
      <td class="ms">${s.durationMs.toFixed(1)} ms</td>
      <td><div class="bar ${cls}" style="margin-left:${left}%;width:${width}%"></div><div class="tags">${tags}</div></td></tr>`)
    walk(s.spanId, depth + 1)
  }
}
walk('root', 0)

const root = (children.get('root') ?? [])[0]
const html = `<!doctype html><html><head><meta charset="utf-8"><title>Trace ${traceId}</title><style>
  body{font:14px system-ui;margin:20px 28px;color:#1c2430;background:#f6f7f9}
  h2{margin:0 0 4px;font-size:20px} .sub{color:#5d6878;margin-bottom:12px}
  pre{background:#111827;color:#d1fae5;padding:10px 14px;border-radius:6px;font:12.5px Consolas,monospace;white-space:pre-wrap;margin:0 0 14px}
  pre .cmd{color:#93c5fd} mark{background:#fde68a;color:#111}
  table{width:100%;border-collapse:collapse;background:#fff;border:1px solid #d8dde4}
  td{border-bottom:1px solid #eef0f3;padding:4px 8px;vertical-align:top}
  td.name{white-space:nowrap;font-weight:600;width:260px} td.ms{width:80px;text-align:right;color:#5d6878;font:12px Consolas,monospace}
  .src{font:11px system-ui;color:#8a94a3;font-weight:400}
  .bar{height:9px;border-radius:3px;margin-top:5px} .bar.app{background:#0b5cad} .bar.db{background:#7c3aed} .bar.http{background:#0a7a32}
  .name.app{color:#0b5cad} .name.db{color:#7c3aed} .name.http{color:#0a7a32}
  .tags{font:11.5px Consolas,monospace;color:#3b4554;margin-top:3px} .err{color:#b42318;font-size:11px}
</style></head><body>
<h2>${escape(root?.name ?? 'Trace')} <span style="font-weight:400;color:#5d6878">${root ? `${root.durationMs.toFixed(1)} ms, ${spans.length} spans` : ''}</span></h2>
<div class="sub">Trace id <mark>${traceId}</mark> (returned to the client in <b>X-Trace-Id</b>). Hover a row for all its tags.</div>
<pre><span class="cmd">$ grep ${traceId} ${escape(file)}</span>
${logs.map((l) => escape(l).replace(traceId, `<mark>${traceId}</mark>`)).join('\n') || '(no log lines)'}</pre>
<table>${rows.join('') || '<tr><td>No spans yet: they are flushed every few seconds.</td></tr>'}</table>
</body></html>`

const out = join(tmpdir(), `trace-${traceId}.html`)
writeFileSync(out, html)
console.log(`${logs.length} log lines, ${spans.length} spans -> ${out}`)
const opener = process.platform === 'win32' ? ['cmd', ['/c', 'start', '', out]] : [process.platform === 'darwin' ? 'open' : 'xdg-open', [out]]
if (!process.env.TRACE_VIEW_NO_OPEN) execFile(...opener)
