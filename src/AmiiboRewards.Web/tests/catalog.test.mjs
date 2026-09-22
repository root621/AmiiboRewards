import { test } from 'node:test'
import assert from 'node:assert/strict'
import { readFileSync } from 'node:fs'
import ts from 'typescript'

const compiled = ts.transpileModule(readFileSync(new URL('../src/catalog.ts', import.meta.url), 'utf8'), { compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext } }).outputText
const { emptyFilters, filterRewards, orderDrops, provider } = await import(`data:text/javascript;base64,${Buffer.from(compiled).toString('base64')}`)

const traveler = { internalId: 'Weapon_Bow_001', name: 'Arco de viajero', description: 'Un arco pequeño.', category: 'Arcos', amiiboCount: 2, amiibo: [
  { name: 'Link arquero', pool: 'BigHit(Normal)', probability: 30 },
  { name: 'Revali', pool: 'BigHit(Normal)', probability: 100 },
] }
const soldier = { ...traveler, internalId: 'Weapon_Bow_002', name: 'Arco de soldado', amiibo: [
  { name: 'Revali', pool: 'GreatHit(Parasail)', probability: 80 },
  { name: 'Link arquero', pool: 'BigHit(Remain)', probability: 25 },
] }
const amber = { internalId: 'Item_Ore_A', name: 'Ámbar', description: 'Resina fósil', category: 'Materiales', amiiboCount: 1, amiibo: [{ name: 'Revali', pool: 'Normal', probability: 20 }] }
const items = [traveler, soldier, amber]

test('buscar ignora acentos y mayúsculas', () => {
  assert.equal(filterRewards(items, { ...emptyFilters, query: 'AMBAR' }, 'name')[0].name, 'Ámbar')
})
test('la búsqueda se combina con la categoría', () => {
  assert.equal(filterRewards(items, { ...emptyFilters, query: 'arco', category: 'Materiales' }, 'name').length, 0)
})
test('todos los criterios deben coincidir en la misma posibilidad', () => {
  assert.equal(filterRewards(items, { ...emptyFilters, amiibo: 'Link arquero', minimum: 50 }, 'name').length, 0)
})
test('las tarjetas muestran solo las posibilidades filtradas', () => {
  const result = filterRewards(items, { ...emptyFilters, amiibo: 'Revali', pool: 'GreatHit', progress: 'Parasail' }, 'name')
  assert.equal(result.length, 1)
  assert.deepEqual(result[0].amiibo, [soldier.amiibo[0]])
})
test('el filtro de etapa incluye recompensas sin condición', () => {
  assert.equal(filterRewards([amber], { ...emptyFilters, progress: 'Parasail' }, 'name').length, 1)
  assert.equal(filterRewards([traveler], { ...emptyFilters, progress: 'Parasail' }, 'name').length, 0)
})
test('ordena por la probabilidad de las posibilidades que cumplen los filtros', () => {
  assert.equal(filterRewards(items, { ...emptyFilters, amiibo: 'Link arquero' }, 'chance')[0].internalId, traveler.internalId)
  assert.equal(orderDrops(traveler.amiibo, 'chance')[0].name, 'Revali')
})
test('filtrar y ordenar no modifica los datos originales', () => {
  const before = JSON.stringify(items)
  filterRewards(items, { ...emptyFilters, amiibo: 'Revali' }, 'chance')
  orderDrops(traveler.amiibo, 'chance')
  assert.equal(JSON.stringify(items), before)
})
test('prefiere el nombre de amiibo del catalogo sobre el selector interno', () => {
  assert.equal(provider({ name: 'Familia Mifar', amiiboDisplayName: 'Mipha', pool: 'Normal', probability: 34 }), 'Mipha')
})
test('ordinary interactions use probability while only explicit specials use the special label', () => {
  assert.equal({ isSpecial: false, probability: 34 }.isSpecial, false)
  assert.equal({ isSpecial: true, probability: 0 }.isSpecial, true)
})
test('cada amiibo concreto llega como una fila independiente, nunca un nombre unido con "/"', () => {
  const reward = { internalId: 'Armor_ArmoredCarp', name: 'Armored Carp', category: 'Otros', amiiboCount: 3, amiibo: [
    { amiiboDisplayName: 'Link (Wind Waker)', pool: 'Normal', probability: 12 },
    { amiiboDisplayName: 'Mipha', pool: 'Normal', probability: 12 },
    { amiiboDisplayName: 'Toon Link', pool: 'Normal', probability: 12 },
  ] }
  for (const drop of reward.amiibo) assert.doesNotMatch(provider(drop), /\s\/\s/)
  assert.equal(new Set(reward.amiibo.map(provider)).size, 3)
})
test('la etiqueta principal de probabilidad nunca incluye "peso"', () => {
  const drop = { amiiboDisplayName: 'Link', pool: 'Normal', probability: 100, rawWeight: 60 }
  assert.doesNotMatch(`${drop.probability}%`, /peso/)
})
