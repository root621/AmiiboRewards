# Amiibo Rewards

Aplicación de búsqueda de recompensas de amiibo de *Breath of the Wild*, con importaciones reproducibles desde una RomFS local.

La solución separa `Domain` (modelo, aliases y Yaz0), `Application` (casos de uso), `Infrastructure` (EF/PostgreSQL), `Api`, `Web` (React/Vite) y herramientas para BOTW y assets.

```bash
docker compose up -d
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/AmiiboRewards.Infrastructure --startup-project src/AmiiboRewards.Api
dotnet run --project src/AmiiboRewards.Api
```

La API queda en `http://localhost:5091` (perfil `http` de `launchSettings.json`). En otra terminal: `cd src/AmiiboRewards.Web && npm run dev`, que sirve la web en `http://localhost:5175` y proxya `/api` hacia el backend (target configurable con `VITE_API_PROXY_TARGET`, ver `.env.example`).

La carpeta de dumps se configura desde el panel **Carpeta de dumps** de la web y queda guardada en PostgreSQL. Solo se aceptan RomFS extraídas; por ejemplo:

```text
/ruta/dumps/
└── breath-of-the-wild/
    └── romfs/
        ├── Actor/Pack/Item_Amiibo_DropTable_*.sbactorpack
        └── Pack/Bootup_*.pack
```

Al guardar la ruta, la aplicación vuelve a detectar los juegos disponibles. Después de una importación exitosa, los datos consultados ya están persistidos en PostgreSQL y el dump puede eliminarse; para actualizar, se vuelve a colocar una RomFS extraída en esa carpeta y se importa nuevamente.

Para iconos, configura `Botw:RomFsPath`, `Botw:BntxExtractorPath` e `ImageMagickExecutable` en `tools/AmiiboRewards.Tools.Assets/appsettings.Development.json` o con variables `AMIIBOREWARDS_Botw__*`. Después:

```bash
dotnet run --project tools/AmiiboRewards.Tools.Assets -- extract-icon Weapon_Bow_017
dotnet run --project tools/AmiiboRewards.Tools.Assets -- extract-icon Animal_Fish_A
dotnet run --project tools/AmiiboRewards.Tools.Assets -- sync-icons
```

Los PNG se crean en `assets/botw/icons`. La herramienta valida Yaz0, el BNTX en offset 4096 y cada proceso externo.

## Biblioteca de juegos

El selector usa la biblioteca persistida (`GET /api/games`), no depende de que el dump siga presente. Al abrir la app, guardar la carpeta o pulsar **Volver a explorar carpeta guardada**, `POST /api/dumps/scan` detecta y registra las RomFS por sus archivos. Acepta una carpeta raíz con varios juegos, `<juego>/romfs` o una RomFS directa. Las copias desconocidas/incompletas se informan y no reciben una identidad inventada.

- BOTW: catálogo/importador nativo disponible; imagen del álbum y tema Bosque.
- TOTK: identificación, registro, selector, imagen propia y tema Ruinas disponibles. **La importación de recompensas TOTK aún no está implementada**; se muestra ese estado y nunca se presentan recompensas de BOTW como si fueran TOTK.
- Nuevos juegos: se agregan mediante definiciones/detectores e importadores específicos. El catálogo, búsqueda, idiomas e historial ya aceptan `?game=CODIGO`; omitirlo conserva BOTW por compatibilidad.

Las imágenes del encabezado son escenas de los álbumes de los juegos, no portadas comerciales. Se conservan en `assets/<juego>/cover.jpg`. La extracción de TOTK requiere `zstd` en el PATH y utiliza `Pack/ZsDic.pack.zs`, su diccionario `zs.zsdic` y `UI/Album/Default_00_Photo.jpg.zs`. Si no puede extraerse, el juego sigue registrado con un fondo de respaldo. Los temas se asignan por juego; no se calculan automáticamente desde la imagen.

La migración `AddGameLibrary` agrega la ruta del dump y la última detección. Borrar un dump no elimina el juego ni las recompensas persistidas. Conservá la base y la carpeta `assets`.

### Corrección de color de iconos

La conversión DDS de 32 bits respeta sus máscaras de canales mediante `DdsRgbaDecoder` antes de enviar RGBA a ImageMagick. Evita la inversión rojo/azul (flechas, chiles, minerales). `sync-icons` regenera una vez los PNG sin marcador `.rgba-v2`, y las siguientes ejecuciones omiten los archivos ya corregidos. Los contenedores sin icono directo continúan pendientes.

## Importar y validar datos BOTW (comandos)

El importador nativo conserva hashes, fuentes de drop, índices y estado de cada ejecución. Lee directamente la RomFS: `sbactorpack → Yaz0 → SARC → bdrop → AAMP C#`, y carga los nombres/descripciones oficiales desde MSBT para el locale indicado:

```bash
AMIIBOREWARDS_Botw__RomFsPath=/ruta/a/romfs \
dotnet run --project tools/AmiiboRewards.Tools.Botw -- import-romfs USes

# Otros locales disponibles en la RomFS, por ejemplo:
dotnet run --project tools/AmiiboRewards.Tools.Botw -- import-romfs EUes
dotnet run --project tools/AmiiboRewards.Tools.Botw -- import-romfs USen

dotnet run --project tools/AmiiboRewards.Tools.Botw -- validate
dotnet run --project tools/AmiiboRewards.Tools.Botw -- import-amiibo-catalog tools/AmiiboRewards.Tools.Botw/amiibo-catalog.botw.json
```

`import-json` sigue disponible solo para compatibilidad con el dataset experimental. El catálogo de amiibo distingue identificaciones confirmadas, el pool común y tablas reservadas; no inventa amiibo para las tablas 004–006.
