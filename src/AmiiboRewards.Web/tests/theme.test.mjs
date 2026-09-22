import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'

const css = readFileSync(new URL('../src/App.css', import.meta.url), 'utf8')
const app = readFileSync(new URL('../src/App.tsx', import.meta.url), 'utf8')

test('BOTW keeps the forest defaults while TOTK overrides shared accents with copper tokens', () => {
  assert.match(css, /:root \{[\s\S]*--control-hover: #2a3930[\s\S]*--category-active: #29372c/)
  assert.match(css, /:root\[data-game-theme="ruins"\] \{[\s\S]*--control-hover: #433426[\s\S]*--category-active-line: #a27949/)
  assert.match(css, /\.category-button\.active \{[^}]*var\(--category-active\)[^}]*var\(--category-active-line\)/)
  assert.match(css, /\.reward-card \{[^}]*var\(--card-line\)/)
})

test('the selected game theme controls the document root', () => {
  assert.match(app, /document\.documentElement\.dataset\.gameTheme = selectedTheme/)
})