import { useEffect, useMemo, useRef, useState } from 'react'
import type { FormEvent, ReactNode } from 'react'
import landscape from './assets/botw-landscape.jpg'
import { costumeProgression, emptyFilters, filterRewards, interactionLabel, isProbabilityMeaningful, orderDrops, poolLabels, poolType, progressLabels, progressOf, provider } from './catalog'
import type { Filters, Reward } from './catalog'
import './App.css'

type Game = { code: string; name: string; shortName: string; theme: string; canImport: boolean; dumpAvailable: boolean; romFsPath?: string; rewardCount: number; locales: string[]; coverUrl?: string; status: string }
const localeLabels: Record<string, string> = { USes: 'Español latinoamericano', EUes: 'Español de España', USen: 'English (US)', EUen: 'English (Europe)' }
const gameStatus = (game: Game) => game.status === 'ready' ? `${game.rewardCount} objetos disponibles` : game.status === 'importer_pending' ? 'Lector de recompensas pendiente' : 'Listo para importar'
const categoryIcons: Record<string, string> = { Arcos: 'bow', Armas: 'sword', Escudos: 'shield', Armaduras: 'shield', 'Comida y fauna': 'leaf', Materiales: 'gem', 'Flechas y objetos': 'bow', 'Trajes / Atuendos': 'costume', 'Interacciones amiibo': 'info', Otros: 'grid' }

function Icon({ name, size = 20 }: { name: string; size?: number }) {
  const paths: Record<string, ReactNode> = {
    search: <><circle cx="10.5" cy="10.5" r="6.5"/><path d="m16 16 5 5"/></>,
    close: <path d="m6 6 12 12M6 18 18 6"/>,
    arrow: <path d="M5 12h14m-5-5 5 5-5 5"/>,
    down: <path d="m6 9 6 6 6-6"/>,
    filter: <><path d="M4 7h16M4 17h16"/><circle cx="9" cy="7" r="2"/><circle cx="15" cy="17" r="2"/></>,
    settings: <><path d="m9 3-1 3-3 1 1 3-2 2 2 2-1 3 3 1 1 3h6l1-3 3-1-1-3 2-2-2-2 1-3-3-1-1-3Z"/><circle cx="12" cy="12" r="3"/></>,
    grid: <><rect x="4" y="4" width="6" height="6" rx="1"/><rect x="14" y="4" width="6" height="6" rx="1"/><rect x="4" y="14" width="6" height="6" rx="1"/><rect x="14" y="14" width="6" height="6" rx="1"/></>,
    bow: <><path d="M5 3c14 1 15 12 16 16L5 3v18M3 13h15m-3-3 3 3-3 3"/></>,
    sword: <><path d="m4 20 4-4m-2-3 5 5M9 14l9-11 3 0v3L10 17"/></>,
    shield: <path d="m12 3 8 3v6c0 5-8 9-8 9s-8-4-8-9V6Z"/>,
    costume: <path d="m8 4 4 3 4-3 4 3-2 4-2-1v10H8V10l-2 1-2-4Z"/>,
    leaf: <><path d="M20 3C6 2 1 11 6 17s15 0 14-14ZM5 20l11-12"/></>,
    gem: <><path d="m3 8 4-5h10l4 5-9 13ZM3 8h18M7 3l5 18 5-18"/></>,
    info: <><circle cx="12" cy="12" r="9"/><path d="M12 11v6M12 7v1"/></>,
  }
  return <svg width={size} height={size} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">{paths[name] ?? paths.grid}</svg>
}

function RewardCard({ reward, dropOrder, game }: { reward: Reward; dropOrder: string; game: string }) {
  const [expanded, setExpanded] = useState(false)
  const [missingIcon, setMissingIcon] = useState(false)
  const drops = orderDrops(reward.amiibo, dropOrder)
  const shown = expanded ? drops : drops.slice(0, 2)
  const count = new Set(drops.map(provider)).size
  const isContainer = ['Barrel', 'BarrelBomb', 'Kibako_Contain_01', 'Obj_BreakBoxIron'].includes(reward.internalId)
  const progression = costumeProgression(reward.metadata)
  return <article className="reward-card">
    <div className="reward-heading">
      <div className="item-image">{missingIcon ? <Icon name={categoryIcons[reward.category]} size={32}/> : <img src={`/api/assets/${game.toLowerCase()}/${reward.internalId}?v=rgba2`} alt="" loading="lazy" onError={() => setMissingIcon(true)}/>}</div>
      <div><span className="item-category">{reward.category}</span><h3>{isContainer ? 'Contenedor especial' : reward.name}</h3></div>
    </div>
    <p className={`description ${expanded ? 'expanded' : ''}`}>{isContainer ? 'Contenedor que puede aparecer como recompensa. No tiene una descripción de objeto en los textos del juego.' : reward.description || 'Este objeto no tiene una descripción disponible en el idioma seleccionado.'}{progression && <><br/><br/>{progression}</>}</p>
    <div className="providers-heading"><span>QUIÉN LO ENTREGA</span><span>{count} {drops.some(drop => /^\d{3}$/.test(provider(drop))) ? 'grupos o tablas' : 'amiibo'}</span></div>
    <div className="drops">{shown.map((drop, index) => <div className="drop" key={`${drop.name}-${drop.pool}-${index}`}>
      <div><strong>{provider(drop)}</strong><span>{poolLabels[poolType(drop.pool)] ?? 'Premio'}{drop.minCount != null && <small> · {drop.minCount === drop.maxCount ? `${drop.minCount} entrega${drop.minCount === 1 ? '' : 's'}` : `${drop.minCount}–${drop.maxCount} entregas`}</small>}{progressOf(drop) && <small> · {progressLabels[progressOf(drop)] ?? 'Condición del juego'}</small>}{drop.condition && <small> · condicional</small>}</span></div>
      {drop.isSpecial ? <b title="Resultado especial">Especial</b> : isProbabilityMeaningful(drop) ? <b title="Probabilidad de esta recompensa">{drop.probability.toLocaleString('es')}<small>%</small></b> : <b title="Interacción determinista">{interactionLabel(drop)}</b>}
    </div>)}</div>
    <button className="expand-drops" onClick={() => setExpanded(!expanded)} aria-expanded={expanded}>{expanded ? 'Ver menos' : drops.length > 2 ? `Ver ${drops.length - 2} ${drops.length === 3 ? 'posibilidad más' : 'posibilidades más'}` : 'Ver descripción completa'}<Icon name="down" size={15}/></button>
  </article>
}

function App() {
  const [games, setGames] = useState<Game[]>([])
  const [selectedGame, setSelectedGame] = useState(() => { try { return localStorage.getItem('amiibo-selected-game') || 'BOTW' } catch { return 'BOTW' } })
  const [locale, setLocale] = useState('USes')
  const [libraryMessage, setLibraryMessage] = useState('')
  const [scanning, setScanning] = useState(false)
  const [libraryLoading, setLibraryLoading] = useState(true)
  const [failedCover, setFailedCover] = useState('')
  const [loadedCatalog, setLoadedCatalog] = useState<{ key: string; items: Reward[]; error: string } | null>(null)
  const [filters, setFilters] = useState<Filters>(emptyFilters)
  const [order, setOrder] = useState('name')
  const [dropOrder, setDropOrder] = useState('chance')
  const [limit, setLimit] = useState(12)
  const [reload, setReload] = useState(0)
  const [filtersOpen, setFiltersOpen] = useState(false)
  const [dumpPath, setDumpPath] = useState('')
  const [dumpMessage, setDumpMessage] = useState('')
  const [configLoading, setConfigLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [importing, setImporting] = useState(false)
  const [importMessage, setImportMessage] = useState('')
  const dialogRef = useRef<HTMLDialogElement>(null)
  const searchRef = useRef<HTMLInputElement>(null)
  const sidebarRef = useRef<HTMLElement>(null)
  const contentRef = useRef<HTMLElement>(null)
  const selected = games.find(game => game.code === selectedGame)
  const registeredCode = selected?.code
  const selectedTheme = selected?.theme ?? 'neutral'
  const locales = selected?.locales ?? []
  const activeLocale = locales.includes(locale) ? locale : locales[0] ?? 'USes'
  const requestKey = `${selectedGame}-${activeLocale}-${reload}`
  const loading = !!selected && loadedCatalog?.key !== requestKey
  const catalog = useMemo(() => loadedCatalog?.key === requestKey ? loadedCatalog.items : [], [loadedCatalog, requestKey])
  const error = loadedCatalog?.key === requestKey ? loadedCatalog.error : ''
  const cover = selectedGame === 'BOTW' ? landscape : selected?.coverUrl

  function updateLibrary(data: Game[]) {
    setGames(data)
    setSelectedGame(current => data.some(game => game.code === current) ? current : data[0]?.code ?? '')
  }
  async function scanLibrary() {
    setScanning(true); setLibraryMessage('')
    try {
      const response = await fetch('/api/dumps/scan', { method: 'POST' })
      if (!response.ok) throw new Error('No se pudo explorar la carpeta. Comprobá que la API esté actualizada y disponible.')
      const body: { games: Game[]; warnings: string[] } = await response.json()
      updateLibrary(body.games)
      setLibraryMessage(body.warnings.join(' ') || `${body.games.length} juegos en la biblioteca. Exploración completada.`)
    } catch (reason) { setLibraryMessage(reason instanceof Error ? reason.message : 'No se pudo explorar la carpeta.') }
    finally { setScanning(false) }
  }

  function changeFilter<K extends keyof Filters>(key: K, value: Filters[K]) {
    setFilters(previous => ({ ...previous, [key]: value }))
    setLimit(12)
  }
  function reset() { setFilters(emptyFilters); setLimit(12) }
  function toggleFilters() {
    setFiltersOpen(!filtersOpen)
    if (!filtersOpen) requestAnimationFrame(() => sidebarRef.current?.scrollIntoView({ block: 'start' }))
  }
  function closeFilters() {
    setFiltersOpen(false)
    requestAnimationFrame(() => contentRef.current?.scrollIntoView({ block: 'start' }))
  }

  useEffect(() => {
    const controller = new AbortController()
    async function loadLibrary() {
      try {
        const response = await fetch('/api/games', { signal: controller.signal })
        if (!response.ok) throw new Error('No se pudo cargar la biblioteca. Reiniciá la API actualizada.')
        updateLibrary(await response.json())
        const scan = await fetch('/api/dumps/scan', { method: 'POST', signal: controller.signal })
        if (scan.ok) {
          const body: { games: Game[]; warnings: string[] } = await scan.json()
          if (!controller.signal.aborted) { updateLibrary(body.games); setLibraryMessage(body.warnings.join(' ')) }
        } else setLibraryMessage('La biblioteca está disponible, pero no se pudo explorar la carpeta de dumps.')
      } catch (reason) { if (!controller.signal.aborted) setLibraryMessage(reason instanceof Error ? reason.message : 'No se pudo cargar la biblioteca.') }
      finally { if (!controller.signal.aborted) setLibraryLoading(false) }
    }
    void loadLibrary()
    return () => controller.abort()
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    if (!registeredCode) {
      return () => controller.abort()
    }
    fetch(`/api/rewards/catalog?game=${encodeURIComponent(selectedGame)}&locale=${encodeURIComponent(activeLocale)}`, { signal: controller.signal })
      .then(async response => { if (!response.ok) throw new Error('No pudimos cargar los objetos. Comprobá que la API esté disponible.'); return response.json() })
      .then((data: Reward[]) => { if (!controller.signal.aborted) setLoadedCatalog({ key: requestKey, items: data, error: '' }) })
      .catch(reason => { if (!controller.signal.aborted) setLoadedCatalog({ key: requestKey, items: [], error: reason instanceof Error ? reason.message : 'No se pudo cargar el catálogo.' }) })
    return () => controller.abort()
  }, [activeLocale, selectedGame, requestKey, registeredCode])

  useEffect(() => {
    if (registeredCode) {
      try { localStorage.setItem('amiibo-selected-game', selectedGame) } catch { /* Storage may be disabled. */ }
      document.documentElement.dataset.gameTheme = selectedTheme
    }
  }, [selectedGame, selectedTheme, registeredCode])

  async function openSettings() {
    dialogRef.current?.showModal(); setConfigLoading(true); setDumpMessage('')
    try {
      const response = await fetch('/api/dumps/config')
      if (!response.ok) throw new Error('No se pudo leer la configuración. Intentá abrirla de nuevo.')
      const body = await response.json(); setDumpPath(body.path)
    } catch (reason) { setDumpMessage(reason instanceof Error ? reason.message : 'No se pudo leer la configuración.') }
    finally { setConfigLoading(false) }
  }
  async function saveSettings(event: FormEvent) {
    event.preventDefault(); setSaving(true); setDumpMessage('')
    try {
      const response = await fetch('/api/dumps/config', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ path: dumpPath }) })
      const body = await response.json()
      if (!response.ok) throw new Error(body.error || 'No se pudo guardar la carpeta.')
      setDumpPath(body.path); setDumpMessage('Carpeta guardada correctamente.')
      await scanLibrary()
    } catch (reason) { setDumpMessage(reason instanceof Error ? reason.message : 'No se pudo guardar la carpeta.') }
    finally { setSaving(false) }
  }

  async function triggerImport(gameCode: string) {
    if (!gameCode) return;
    setImporting(true); setImportMessage('');
    try {
      const response = await fetch('/api/dumps/import', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code: gameCode, locale: activeLocale })
      });
      const body = await response.json();
      if (!response.ok) throw new Error(body.error || 'No se pudo iniciar la importación.');
      setImportMessage('Importación iniciada. Esperando los resultados…');
      for (let attempt = 0; attempt < 60; attempt++) {
        await new Promise(resolve => window.setTimeout(resolve, 1000));
        const library = await fetch('/api/games');
        if (!library.ok) continue;
        const updated: Game[] = await library.json();
        const imported = updated.find(game => game.code === gameCode);
        if (imported && imported.rewardCount > 0) {
          updateLibrary(updated);
          setReload(value => value + 1);
          setImportMessage('Importación completada. El catálogo ya está disponible.');
          return;
        }
      }
      setImportMessage('La importación sigue en curso. Volvé a explorar la biblioteca en unos segundos.');
    } catch (reason) {
      setImportMessage(reason instanceof Error ? reason.message : 'No se pudo iniciar la importación.');
    } finally {
      setImporting(false);
    }
  }

  const hasProbability = catalog.some(item => item.amiibo.some(isProbabilityMeaningful))
  const effectiveOrder = !hasProbability && order === 'chance' ? 'name' : order
  const effectiveDropOrder = !hasProbability && dropOrder === 'chance' ? 'amiibo' : dropOrder
  const displayed = useMemo(() => filterRewards(catalog, filters, effectiveOrder), [catalog, filters, effectiveOrder])
  const categories = [...new Set(catalog.map(item => item.category))].sort((a, b) => a.localeCompare(b, 'es'))
  const amiibos = [...new Set(catalog.flatMap(item => item.amiibo.map(drop => provider(drop))))].sort((a, b) => a.localeCompare(b, 'es'))
  const amiiboCount = amiibos.filter(name => !/^\d{3}$/.test(name)).length
  const chips = [
    filters.query && { key: 'query' as const, label: `“${filters.query}”` },
    filters.category && { key: 'category' as const, label: filters.category },
    filters.amiibo && { key: 'amiibo' as const, label: filters.amiibo },
    filters.pool && { key: 'pool' as const, label: poolLabels[filters.pool] },
    filters.progress && { key: 'progress' as const, label: progressLabels[filters.progress] },
    filters.minimum > 0 && { key: 'minimum' as const, label: `≥ ${filters.minimum}%` },
  ].filter(Boolean) as { key: keyof Filters; label: string }[]

  return <div className="app">
    <header className="app-bar">
      <a className="brand" href="#catalog"><span className="brand-symbol"><Icon name="gem" size={22}/></span>Amiibo<span>Rewards</span></a>
      <div className="app-actions">
        <label className="locale-control"><span>Idioma de objetos</span><select aria-label="Idioma de objetos" value={activeLocale} disabled={!locales.length} onChange={event => { setLocale(event.target.value); setLimit(12) }}>{locales.length ? locales.map(item => <option key={item} value={item}>{localeLabels[item] ?? item}</option>) : <option value="USes">Sin textos importados</option>}</select></label>
        <button className="settings-button" aria-label="Configuración" onClick={openSettings}><Icon name="settings" size={18}/><span>Configuración</span></button>
      </div>
    </header>

    <main>
      <section className={`game-banner ${cover && failedCover !== cover ? '' : 'no-art'}`} aria-labelledby="game-title">
        {cover && failedCover !== cover && <img className="game-art" src={cover} alt={`Imagen de ${selected?.shortName ?? 'juego'}`} onError={() => setFailedCover(cover)}/>}
        <div className="game-copy">
          <div className="eyebrow"><span className="status-dot"/>GUÍA DE RECOMPENSAS AMIIBO</div>
          <h1 id="game-title">{selected?.name.startsWith('The Legend of Zelda:') ? <><span>The Legend of Zelda</span>{selected.shortName}</> : selected?.name ?? 'Tu biblioteca de juegos'}</h1>
          <p>Descubrí qué podés conseguir y qué amiibo lo entrega.</p>
          {selected && <div className="game-stats"><span>{gameStatus(selected)}</span><i/><span>{selected.dumpAvailable ? 'RomFS disponible' : 'Sin dump · datos conservados'}</span>{!loading && catalog.length > 0 && <><i/><span><b>{amiiboCount}</b> amiibo y grupos</span></>}</div>}
        </div>
        <label className="game-picker"><span>Juego seleccionado</span><select aria-label="Juego seleccionado" disabled={!games.length} value={selectedGame} onChange={event => { setSelectedGame(event.target.value); setFiltersOpen(false); reset() }}>{games.length ? games.map(game => <option key={game.code} value={game.code}>{game.shortName}</option>) : <option value="">Sin juegos registrados</option>}</select></label>
      </section>

      {!selected || selected.rewardCount === 0 ? <section className="library-state" id="catalog"><Icon name={selected?.status === 'importer_pending' ? 'settings' : 'grid'} size={34}/><h2>{libraryLoading ? 'Buscando tus juegos…' : selected?.status === 'importer_pending' ? `${selected.shortName} detectado` : selected ? 'Juego registrado en tu biblioteca' : 'Agregá tu primer juego'}</h2><p>{libraryLoading ? 'Leyendo la biblioteca y la carpeta de dumps.' : selected?.status === 'importer_pending' ? 'Podés seleccionarlo y conservarlo en la biblioteca. Su formato de recompensas necesita un lector específico que aún no está implementado; todavía no hay objetos importados para este juego.' : selected ? 'La RomFS está identificada. Importá sus recompensas para habilitar el catálogo.' : libraryMessage || 'Configurá la carpeta de RomFS extraídas y explorala para registrar los juegos.'}</p>{selectedGame === 'TOTK' && !cover && <small>Imagen del juego pendiente. Se muestra su tema de respaldo.</small>}{selected?.canImport && <div className="library-actions"><button onClick={() => triggerImport(selected.code)} disabled={importing}>{importing ? 'Importando…' : `Importar ${selected.shortName}`}</button>{importMessage && <p role="status" className="dump-message">{importMessage}</p>}</div>}<button onClick={openSettings}>Administrar biblioteca</button></section> : <div className="workspace" id="catalog">
        <aside ref={sidebarRef} className={`filter-sidebar ${filtersOpen ? 'is-open' : ''}`} id="catalog-filters" aria-label="Filtros del catálogo">
          <div className="filter-title"><h2><Icon name="filter" size={18}/>Filtros</h2><button className="text-button" onClick={reset} disabled={!chips.length}>Limpiar</button></div>
          <fieldset className="category-filter"><legend>Categoría</legend>
            <button className={!filters.category ? 'category-button active' : 'category-button'} onClick={() => changeFilter('category', '')} aria-pressed={!filters.category}><Icon name="grid" size={18}/><span>Todos los objetos</span><b>{catalog.length}</b></button>
            {categories.map(category => <button key={category} className={filters.category === category ? 'category-button active' : 'category-button'} onClick={() => changeFilter('category', category)} aria-pressed={filters.category === category}><Icon name={categoryIcons[category]} size={18}/><span>{category}</span><b>{catalog.filter(item => item.category === category).length}</b></button>)}
          </fieldset>
          <div className="filter-fields">
            <label>Amiibo<select value={filters.amiibo} onChange={event => changeFilter('amiibo', event.target.value)}><option value="">Todos los amiibo</option>{amiibos.map(name => <option key={name} value={name}>{/^\d{3}$/.test(name) ? `Tabla sin identificar (${name})` : name}</option>)}</select></label>
            <label>Tipo de recompensa<select value={filters.pool} onChange={event => changeFilter('pool', event.target.value)}><option value="">Todas las recompensas</option>{Object.entries(poolLabels).map(([key, value]) => <option key={key} value={key}>{value}</option>)}</select></label>
            <label>Etapa del juego<select value={filters.progress} onChange={event => changeFilter('progress', event.target.value)}><option value="">Todas las etapas</option>{Object.entries(progressLabels).map(([key, value]) => <option key={key} value={key}>{value}</option>)}</select><small>Incluye objetos sin condición de progreso.</small></label>
            {hasProbability && <label>Probabilidad mínima<select value={filters.minimum} onChange={event => changeFilter('minimum', Number(event.target.value))}><option value="0">Cualquier probabilidad</option>{[10, 30, 50, 100].map(value => <option key={value} value={value}>{value}% o más</option>)}</select></label>}
          </div>
          <p className="filter-hint"><Icon name="info" size={16}/>Los filtros se combinan y se aplican al instante.</p>
          <button className="apply-mobile-filters" onClick={closeFilters}>Ver {displayed.length} objetos<Icon name="arrow" size={17}/></button>
        </aside>

        <section ref={contentRef} className="catalog-content" aria-label="Objetos y recompensas">
          <div className="catalog-heading"><div><p className="eyebrow">EXPLORÁ EL CATÁLOGO</p><h2>Encontrá tu próxima recompensa</h2></div></div>
          <form className="search-form" role="search" onSubmit={event => { event.preventDefault(); searchRef.current?.focus() }}>
            <Icon name="search" size={21}/><input ref={searchRef} aria-label="Buscar objetos" type="search" value={filters.query} onChange={event => changeFilter('query', event.target.value)} placeholder="Buscá arcos, armaduras, ingredientes…" autoComplete="off"/>
            {filters.query && <button type="button" className="clear-search" aria-label="Borrar búsqueda" onClick={() => { changeFilter('query', ''); searchRef.current?.focus() }}><Icon name="close" size={16}/></button>}
            <button type="submit" className="search-submit">Buscar</button>
          </form>
          <div className="results-toolbar">
            <button className="mobile-filters" aria-expanded={filtersOpen} aria-controls="catalog-filters" onClick={toggleFilters}><Icon name="filter" size={17}/>Filtros {chips.length > 0 && `(${chips.length})`}</button>
            <p role="status" aria-live="polite">{loading ? 'Cargando objetos…' : <><b>{displayed.length}</b> {displayed.length === 1 ? 'objeto' : 'objetos'}<span> de {catalog.length}</span></>}</p>
            <label className="sort-control">Ordenar<select value={effectiveOrder} onChange={event => { setOrder(event.target.value); setLimit(12) }}><option value="name">Nombre A–Z</option>{hasProbability && <option value="chance">Mayor probabilidad</option>}<option value="amiibo">Más amiibo</option></select></label>
          </div>
          {chips.length > 0 && <div className="active-filters" aria-label="Filtros activos">{chips.map(chip => <button key={chip.key} onClick={() => changeFilter(chip.key, chip.key === 'minimum' ? 0 : '')} aria-label={`Quitar filtro ${chip.label}`}>{chip.label}<Icon name="close" size={13}/></button>)}<button className="reset-filters" onClick={reset}>Limpiar todo</button></div>}
          <div className="probability-bar">{hasProbability && <details><summary><Icon name="info" size={15}/>Cómo leer los porcentajes</summary><p>Cada porcentaje corresponde al objeto dentro de su lista de recompensas, no a la probabilidad total de un escaneo. Las etapas y los tipos de premio son condiciones distintas; sus porcentajes no se suman.</p></details>}<label>{hasProbability ? 'Posibilidades' : 'Interacciones'}<select aria-label="Orden de posibilidades" value={effectiveDropOrder} onChange={event => setDropOrder(event.target.value)}>{hasProbability && <option value="chance">Mayor % primero</option>}<option value="amiibo">Por amiibo</option><option value="pool">Por tipo de premio</option></select></label></div>

          {loading ? <div className="results skeletons" aria-hidden="true">{Array.from({ length: 6 }, (_, i) => <div className="skeleton-card" key={i}><div/><span/><span/><span/></div>)}</div> :
            error ? <div className="empty-state" role="alert"><Icon name="info" size={32}/><h3>No pudimos mostrar el catálogo</h3><p>{error}</p><button onClick={() => setReload(value => value + 1)}>Reintentar</button></div> :
            displayed.length === 0 ? <div className="empty-state"><Icon name="search" size={36}/><h3>{catalog.length ? 'No encontramos coincidencias' : 'Tu catálogo todavía está vacío'}</h3><p>{catalog.length ? 'Probá con otro nombre o quitá un filtro para ampliar la búsqueda.' : 'Importá los datos de un juego para explorar sus recompensas.'}</p>{catalog.length > 0 && <button onClick={reset}>Ver todos los objetos</button>}</div> :
            <><div className="results">{displayed.slice(0, limit).map(reward => <RewardCard key={`${selectedGame}-${locale}-${reward.internalId}-${JSON.stringify(filters)}`} reward={reward} dropOrder={effectiveDropOrder} game={selectedGame}/>)}</div><div className="pagination"><span>Mostrando {Math.min(limit, displayed.length)} de {displayed.length} objetos</span>{limit < displayed.length && <button onClick={() => setLimit(value => value + 12)}>Mostrar más objetos<Icon name="down" size={16}/></button>}</div></>}
        </section>
      </div>}
    </main>
    <footer className="app-footer"><span>Amiibo Rewards</span><span>Tu guía para descubrir cada recompensa.</span></footer>
    <dialog ref={dialogRef} className="settings-dialog" aria-labelledby="settings-title" onClick={event => { if (event.target === event.currentTarget) dialogRef.current?.close() }}>
      <div className="dialog-header"><div><p className="eyebrow">CONFIGURACIÓN</p><h2 id="settings-title">Carpeta de dumps</h2></div><button className="icon-button" aria-label="Cerrar configuración" onClick={() => dialogRef.current?.close()}><Icon name="close"/></button></div>
      <p>Indicá la carpeta del equipo donde se ejecuta la API que contiene tus RomFS extraídas.</p>
      <form onSubmit={saveSettings}><label>Ruta de la carpeta<input autoFocus value={dumpPath} onChange={event => setDumpPath(event.target.value)} disabled={configLoading || saving} placeholder="/ruta/a/dumps" required/></label><p className="settings-note">Los objetos importados siguen disponibles aunque retires el dump. Conservá también los recursos de imágenes de la aplicación.</p><p role="status" className="dump-message">{configLoading ? 'Leyendo configuración…' : dumpMessage}</p><div className="dialog-actions"><button type="button" className="secondary" onClick={() => dialogRef.current?.close()}>Cerrar</button><button disabled={configLoading || saving || !dumpPath.trim()}>{saving ? 'Guardando…' : 'Guardar cambios'}</button></div></form>
      <section className="library-management" aria-label="Biblioteca de juegos"><div><h3>Juegos registrados</h3><button onClick={scanLibrary} disabled={scanning || saving}>{scanning ? 'Explorando…' : 'Volver a explorar carpeta guardada'}</button></div><p role="status">{libraryMessage}</p><ul>{games.map(game => <li key={game.code}><strong>{game.shortName}</strong><span>{gameStatus(game)} · {game.dumpAvailable ? 'Dump disponible' : 'Dump no disponible'}</span>{game.romFsPath && <small>{game.romFsPath}</small>}</li>)}</ul></section>
    </dialog>
  </div>
}
export default App
