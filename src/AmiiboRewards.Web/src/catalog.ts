export type Drop = { name?: string; amiiboDisplayName?: string; selectorKind?: string; selectorValue?: string; mappingStatus?: string | number; pool: string; probability: number; normalizedProbability?: number | null; rawWeight?: number; minCount?: number; maxCount?: number; condition?: string; isConditional?: boolean; interactionKind?: string | number; outcomeKind?: string | number; isSpecial?: boolean; specialMetadata?: string }
export type Reward = { internalId: string; name: string; description?: string; metadata?: string; category: string; amiiboCount: number; amiibo: Drop[] }
export type Filters = { query: string; category: string; amiibo: string; pool: string; progress: string; minimum: number }
export const emptyFilters: Filters = { query: '', category: '', amiibo: '', pool: '', progress: '', minimum: 0 }
export const normalize = (value: string) => value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase().trim()
export const poolType = (pool: string) => pool.split('(')[0]
export const progressOf = (drop: Drop) => drop.pool.match(/\(([^)]+)\)/)?.[1] ?? drop.condition ?? ''
export const poolLabels: Record<string, string> = { Normal: 'Objetos en el suelo', SmallHit: 'Lote extra', BigHit: 'Cofre normal', GreatHit: 'Cofre excepcional', Special: 'Resultado especial', AmiiboUnlock: 'Desbloqueo anticipado', HintSearch: 'Búsqueda de pistas' }
export const progressLabels: Record<string, string> = { Normal: 'Antes de la paravela', Parasail: 'Con paravela', Remain: 'Tras una bestia divina' }
export const isProbabilityMeaningful = (drop: Drop) => drop.interactionKind == null || drop.interactionKind === 'Drop' || drop.interactionKind === 0
export const interactionLabel = (drop: Drop) => drop.interactionKind === 'Unlock' || drop.interactionKind === 1 ? 'Desbloqueo garantizado' : 'Interacción especial'
export function costumeProgression(metadata?: string) {
  if (!metadata) return ''
  try {
    const parsed = JSON.parse(metadata) as { NormalRoutes?: { ItemType: string; MoonNum?: number; ClearWorld?: string; Price?: number }[] }
    const routes = parsed.NormalRoutes ?? []
    if (!routes.length) return 'ItemList.byml no expone una ruta normal para este atuendo.'
    const details = routes.map(route => {
      const item = route.ItemType === 'Cap' ? 'gorra' : route.ItemType === 'Clothes' ? 'traje' : 'pieza'
      if (route.MoonNum != null) return `${item}: ${route.MoonNum} energilunas${route.Price ? ` y ${route.Price} monedas` : ''}`
      if (route.ClearWorld) return `${item}: tras completar el reino indicado por el juego${route.Price ? `, por ${route.Price} monedas` : ''}`
      return `${item}${route.Price ? `: ${route.Price} monedas` : ''}`
    })
    return `El amiibo lo desbloquea antes. Ruta normal: ${details.join('; ')}.`
  } catch { return '' }
}
export function provider(drop: Drop) {
  if (drop.mappingStatus === 'Reserved' || drop.mappingStatus === 3) return 'Tabla reservada'
  if (drop.name && /^\d{3}$/.test(drop.name)) return 'Amiibo sin identificar'
  return drop.amiiboDisplayName ?? drop.name ?? 'Amiibo sin identificar'
}
export function filterRewards(rewards: Reward[], filters: Filters, order: string): Reward[] {
  const words = normalize(filters.query).split(/\s+/).filter(Boolean)
  return rewards.flatMap(item => {
    if (filters.category && item.category !== filters.category) return []
    const haystack = normalize(`${item.name} ${item.description ?? ''} ${item.internalId}`)
    if (!words.every(word => haystack.includes(word))) return []
    const drops = item.amiibo.filter(drop =>
      (!filters.amiibo || provider(drop) === filters.amiibo) &&
      (!filters.pool || poolType(drop.pool) === filters.pool) &&
      (!filters.progress || progressOf(drop) === filters.progress || !progressOf(drop)) &&
      drop.probability >= filters.minimum)
    return drops.length ? [{ ...item, amiibo: drops }] : []
  }).sort((a, b) => {
    const byName = a.name.localeCompare(b.name, 'es')
    if (order === 'chance') return Math.max(...b.amiibo.map(d => d.probability)) - Math.max(...a.amiibo.map(d => d.probability)) || byName
    if (order === 'amiibo') return new Set(b.amiibo.map(provider)).size - new Set(a.amiibo.map(provider)).size || byName
    return byName
  })
}
export function orderDrops(drops: Drop[], order: string) {
  return [...drops].sort((a, b) => {
    if (order === 'amiibo') return provider(a).localeCompare(provider(b), 'es') || b.probability - a.probability
    if (order === 'pool') return a.pool.localeCompare(b.pool) || b.probability - a.probability
    return b.probability - a.probability || provider(a).localeCompare(provider(b), 'es')
  })
}
