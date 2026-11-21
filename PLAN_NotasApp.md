# Plan de trabajo — App personal de notas, reuniones y código

## 1. Objetivo

App de escritorio de uso personal para:

- **Notas**: escribir notas para no olvidar cosas o guardar información relevante.
- **Reuniones**: agendar reuniones con fecha, hora elegida de forma interactiva y una nota, y recibir un recordatorio.
- **Archivos de código**: crear archivos con nombre, escribir porciones de código dentro y agruparlos por categoría (SQL, CSS, etc.).

Requisitos generales:

- Marcar notas y reuniones como **completadas**.
- **Arrastrar** para ordenar notas, reuniones y archivos, con animaciones fluidas.
- **Filtrar** por día, rango de fechas, tipo y estado.
- Interfaz al **estilo Apple**, en **negro y gris**.
- Datos guardados en **Neon (PostgreSQL)**.
- **Modo offline**: la app funciona sin internet y sincroniza con Neon cuando vuelve la conexión.
- **Búsqueda global** en notas, reuniones y archivos de código.
- **Atajos de teclado** dentro de la app y un **atajo global** para abrirla desde cualquier lugar.
- **Exportar e importar** archivos de código.
- **Autoguardado** en el editor de código y **botón Copiar** en cada archivo.
- **Favoritos y etiquetas** para notas, reuniones y archivos.
- **Posponer avisos** ("recordar en 5 minutos") desde la notificación.
- **Actualizaciones automáticas** de la app.
- **Ejecución en segundo plano**: al cerrar la ventana, la app sigue corriendo en la bandeja del sistema.
- **Inicio con Windows**: la app arranca sola al encender la computadora, ya en segundo plano.
- **Avisos de reuniones**: notificación de Windows cuando una reunión está cerca y cuando llega la hora.
- **Instalador descargable**: un archivo `.exe` para instalar la app en la computadora.

---

## 2. Stack tecnológico

| Capa | Tecnología |
|---|---|
| Shell de escritorio | **WPF (.NET 10)** + `BlazorWebView` (WebView2) |
| UI | **Blazor (Razor + HTML + CSS)** |
| Editor de código | **BlazorMonaco** (el editor de VS Code) |
| Arrastrar y ordenar | **SortableJS** mediante JS interop |
| Animaciones | Transiciones y animaciones CSS |
| Base de datos local | **SQLite** con EF Core (`Microsoft.EntityFrameworkCore.Sqlite`) |
| Base de datos en la nube | **Neon** (PostgreSQL serverless) con EF Core + **Npgsql** |
| Búsqueda | **SQLite FTS5** (búsqueda de texto completo, funciona sin internet) |
| Notificaciones | Notificaciones *toast* de Windows (`CommunityToolkit.WinUI.Notifications`) |
| Bandeja del sistema | `H.NotifyIcon.Wpf` (ícono junto al reloj, con menú) |
| Atajo global | API de Windows `RegisterHotKey` (o el paquete `NHotkey.Wpf`) |
| Inicio con Windows | Entrada en el registro `HKCU\...\CurrentVersion\Run` |
| Instancia única | `Mutex` (evita abrir la app dos veces) |
| Instalador y actualizaciones | **Velopack** (genera el `Setup.exe` y actualiza la app sola) |
| Publicación de versiones | **GitHub Releases** (de donde la app descarga las actualizaciones) |

**Configuración:** la cadena de conexión de Neon no se escribe en el código. En desarrollo va en *User Secrets*, y en la app instalada se guarda cifrada en el equipo (DPAPI).

---

## 3. Diseño visual

Paleta oscura inspirada en iOS y macOS:

| Token | Color | Uso |
|---|---|---|
| `--bg` | `#000000` | Fondo de la ventana |
| `--surface` | `#1C1C1E` | Sidebar y paneles |
| `--card` | `#2C2C2E` | Tarjetas |
| `--card-hover` | `#3A3A3C` | Hover y elemento seleccionado |
| `--border` | `rgba(255,255,255,0.08)` | Bordes finos |
| `--text` | `#F5F5F7` | Texto principal |
| `--text-2` | `#8E8E93` | Texto secundario |
| `--accent` | `#0A84FF` | Acciones y foco |
| `--success` | `#30D158` | Completado |

Reglas de estilo:

- Esquinas redondeadas de 12 a 16 px, bordes de 1 px y sombras suaves.
- `backdrop-filter: blur()` en el sidebar y los modales.
- Fuente del sistema: `Segoe UI Variable`, con `-apple-system` e Inter como respaldo.
- Animaciones cortas (150 a 250 ms) con *easing* suave al crear, eliminar, completar y reordenar.

---

## 4. Modelo de datos

Las mismas tablas existen en **SQLite (local)** y en **Neon (nube)**. Los `id` son `uuid` generados en la app, así un registro creado sin internet conserva su identidad al sincronizar.

```sql
-- Notas y reuniones
items (
  id             uuid PK,
  tipo           text,          -- 'nota' | 'reunion'
  titulo         text,
  contenido      text,          -- texto de la nota o nota de la reunión
  fecha          date,          -- día al que pertenece
  hora           time NULL,     -- solo para reuniones
  completado     boolean,
  favorito       boolean,
  etiquetas      text,          -- lista en JSON: ["trabajo","urgente"] (jsonb en Neon)
  posponer_hasta timestamptz NULL, -- si se pospuso el aviso de una reunión
  orden          int,           -- posición tras arrastrar
  creado_en      timestamptz,
  actualizado_en timestamptz,
  eliminado      boolean        -- borrado lógico, para sincronizar eliminaciones
)

-- Categorías de código (SQL, CSS, C#, ...)
categorias (
  id             uuid PK,
  nombre         text,          -- 'SQL', 'CSS', ...
  lenguaje       text,          -- id de lenguaje en Monaco: 'sql', 'css', 'csharp'
  extension      text,          -- '.sql', '.css', '.cs' (para exportar)
  orden          int,
  actualizado_en timestamptz,
  eliminado      boolean
)

-- Archivos de código
archivos (
  id             uuid PK,
  nombre         text,
  categoria_id   uuid FK -> categorias,
  contenido      text,
  favorito       boolean,
  etiquetas      text,          -- lista en JSON (jsonb en Neon)
  orden          int,
  creado_en      timestamptz,
  actualizado_en timestamptz,
  eliminado      boolean
)
```

Solo en SQLite:

```sql
-- Cambios pendientes de subir a Neon
sync_pendientes (
  id          integer PK,
  tabla       text,
  registro_id uuid,
  creado_en   timestamptz
)

-- Índice de búsqueda de texto completo (FTS5) sobre títulos, contenidos y nombres
busqueda (tipo, registro_id, titulo, contenido)
```

---

## 5. Estructura de la interfaz

```
┌──────────────┬──────────────────────────────────────┐
│ Sidebar      │  Barra superior: filtros + "Nuevo"   │
│  [Buscar]    ├──────────────────────────────────────┤
│  Favoritos   │                                      │
│  Notas       │                                      │
│  Reuniones   │  Lista de tarjetas (arrastrables)    │
│  Código      │   o                                  │
│   ├ SQL      │  Lista de archivos + editor Monaco   │
│   ├ CSS      │                                      │
│   └ ...      │                                      │
│  ● En línea  │                                      │
└──────────────┴──────────────────────────────────────┘
```

El indicador inferior muestra el estado de conexión: **En línea**, **Sin conexión** o **Sincronizando**.

---

## 6. Fases de trabajo

### Fase 0 — Configuración del proyecto
- Crear la solución con un proyecto WPF y `BlazorWebView`.
- Crear el proyecto y la base de datos en Neon.
- Configurar EF Core con **dos contextos**: SQLite (local) y Npgsql (Neon), con el mismo modelo.
- Crear las migraciones iniciales en ambas bases.
- Guardar la cadena de conexión fuera del código.

**Listo cuando:** la app abre una ventana con Blazor, lee y escribe en SQLite y se conecta a Neon.

### Fase 1 — Layout y tema
- Crear las variables CSS de la paleta y los estilos base.
- Hacer el sidebar (Notas, Reuniones, Código) y la barra superior.
- Configurar la navegación entre secciones.

**Listo cuando:** se navega entre las tres secciones con el tema negro y gris aplicado.

### Fase 2 — Notas
- Crear, editar y eliminar notas.
- Marcar y desmarcar una nota como completada.
- Arrastrar para reordenar notas y guardar el nuevo `orden`.

**Listo cuando:** el orden y el estado se conservan al cerrar y volver a abrir la app.

### Fase 3 — Reuniones
- Crear, editar y eliminar reuniones con título, fecha, hora y nota.
- Agregar un selector de hora interactivo (rueda o lista desplazable al estilo iOS).
- Marcar como completada.
- Arrastrar para reordenar.

**Listo cuando:** se puede agendar una reunión eligiendo la hora visualmente y queda guardada.

### Fase 4 — Filtros
- Filtrar por **Hoy**, **Mañana**, **Esta semana** y **un día o rango específico** (calendario).
- Filtrar por **tipo** (notas o reuniones).
- Filtrar por **estado** (pendientes, completadas o todas).

**Listo cuando:** los filtros se pueden combinar y la lista se actualiza con animación.

### Fase 5 — Segundo plano, inicio con Windows y avisos
- **Bandeja del sistema**: al cerrar la ventana, la app se oculta junto al reloj en vez de cerrarse. El menú del ícono tiene "Abrir" y "Salir".
- **Inicio con Windows**: opción en la configuración (activada por defecto) que registra la app para arrancar al encender la computadora. Al iniciar así, arranca oculta en la bandeja.
- **Instancia única**: si la app ya está corriendo y se vuelve a abrir, se muestra la ventana existente.
- **Servicio de avisos**: un temporizador revisa las reuniones pendientes cada minuto, aunque la ventana esté cerrada.
  - **Aviso previo**: "Tu reunión empieza en X minutos". Por defecto 10 minutos antes, configurable.
  - **Aviso a la hora**: "Tu reunión empieza ahora".
  - No se repite un aviso ya mostrado ni se avisa de reuniones completadas.
  - Al hacer clic en la notificación, se abre la app en esa reunión.
  - Funciona sin internet, porque lee las reuniones de SQLite.
- **Posponer aviso**: la notificación tiene botones "Recordar en 5 min", "Recordar en 10 min" y "Descartar". Al posponer, se guarda `posponer_hasta` y el aviso vuelve a esa hora.

**Listo cuando:** con la ventana cerrada y tras reiniciar la computadora, llegan los dos avisos de una reunión programada, y "Recordar en 5 min" la vuelve a avisar 5 minutos después.

### Fase 6 — Archivos de código
- Crear, editar y eliminar **categorías** (SQL, CSS, etc.) y asignarles un lenguaje para el resaltado y una extensión.
- Crear archivos con **nombre** dentro de una categoría.
- Editar el contenido con **Monaco**, con resaltado según el lenguaje de la categoría.
- **Autoguardado**: guarda automáticamente un segundo después de dejar de escribir y muestra "Guardado" en la barra del editor. `Ctrl+S` sigue funcionando para guardar al instante.
- **Botón Copiar**: copia todo el contenido del archivo al portapapeles y muestra "Copiado".
- Agrupar los archivos por categoría en el sidebar.
- Arrastrar para ordenar archivos y categorías.

**Listo cuando:** se crea un archivo SQL, se escribe código con resaltado, se guarda solo, se copia con un clic y aparece agrupado bajo "SQL".

### Fase 7 — Modo offline y sincronización
- La UI siempre lee y escribe en **SQLite**, así responde al instante con o sin internet.
- Cada cambio (crear, editar, completar, reordenar, eliminar) se registra en `sync_pendientes`.
- Un **servicio de sincronización** en segundo plano:
  - **Sube** los cambios pendientes a Neon cuando hay conexión.
  - **Baja** los cambios hechos en Neon desde la última sincronización (por `actualizado_en`).
  - Reintenta automáticamente al recuperar la conexión.
  - En caso de conflicto, gana el cambio más reciente (`actualizado_en`).
- Las eliminaciones son lógicas (`eliminado = true`), para que se propaguen.
- Indicador de estado en el sidebar: **En línea**, **Sin conexión** o **Sincronizando**.

**Listo cuando:** sin internet se crean y editan notas, reuniones y archivos, y al reconectar todo aparece en Neon sin perder datos.

### Fase 8 — Búsqueda global
- Abrir un buscador rápido con `Ctrl+K` (o desde "Buscar" en el sidebar).
- Buscar en títulos y contenido de **notas**, **reuniones** y **archivos de código**, en una sola lista.
- Mostrar resultados agrupados por tipo, con el texto encontrado resaltado.
- Navegar con las flechas y abrir con `Enter`.
- Usar el índice **FTS5** de SQLite, que funciona sin internet y se actualiza con cada cambio.

**Listo cuando:** al escribir una palabra aparecen al instante las notas, reuniones y archivos que la contienen.

### Fase 9 — Atajos de teclado
**Atajo global** (funciona aunque la app esté en segundo plano o minimizada):

| Atajo | Acción |
|---|---|
| `Ctrl+Alt+N` | Mostrar u ocultar la app (configurable) |

**Atajos dentro de la app:**

| Atajo | Acción |
|---|---|
| `Ctrl+N` | Nuevo elemento en la sección actual |
| `Ctrl+K` | Búsqueda global |
| `Ctrl+S` | Guardar archivo de código |
| `Ctrl+E` | Exportar el archivo actual |
| `Ctrl+Shift+C` | Copiar el contenido del archivo actual |
| `Ctrl+D` | Marcar o quitar de favoritos |
| `Ctrl+1` / `Ctrl+2` / `Ctrl+3` | Ir a Notas / Reuniones / Código |
| `Ctrl+Enter` | Marcar el elemento seleccionado como completado |
| `Supr` | Eliminar el elemento seleccionado (con confirmación) |
| `Esc` | Cerrar modal o buscador |

- Mostrar una pantalla con la lista de atajos (`Ctrl+/`).
- Permitir cambiar el atajo global en la configuración y avisar si otra aplicación ya lo usa.

**Listo cuando:** con la app en la bandeja, `Ctrl+Alt+N` la abre, y todos los atajos internos funcionan.

### Fase 10 — Exportar e importar
**Exportar:**
- **Un archivo**: guardarlo con su extensión según la categoría (`consulta.sql`, `estilos.css`).
- **Una categoría**: exportar todos sus archivos en un `.zip`.
- **Todo el código**: exportar todas las categorías en un `.zip`, con una carpeta por categoría.
- Usar el diálogo nativo de Windows "Guardar como".

**Importar:**
- Elegir uno o varios archivos (`.sql`, `.css`, `.cs`, etc.) o arrastrarlos sobre la app.
- Asignar la categoría según la extensión, con opción de cambiarla antes de confirmar.
- Importar un `.zip` exportado por la app, respetando las carpetas como categorías.
- Si ya existe un archivo con el mismo nombre en la categoría, preguntar si se reemplaza o se conserva con otro nombre.

**Listo cuando:** se exporta un archivo, una categoría y todo el código, y al importar ese `.zip` se recuperan los mismos archivos en sus categorías.

### Fase 11 — Favoritos y etiquetas
- Marcar notas, reuniones y archivos como **favoritos** con una estrella (o `Ctrl+D`).
- Agregar una sección **Favoritos** en el sidebar.
- Agregar **etiquetas** libres a cualquier elemento ("trabajo", "urgente", "cliente X"), con autocompletado de las ya usadas.
- Mostrar las etiquetas como chips en las tarjetas y en la lista de archivos.
- **Filtrar** por favorito y por etiqueta, combinable con los filtros de la Fase 4.
- Incluir las etiquetas en la **búsqueda global**.

**Listo cuando:** se marca un elemento como favorito, se le pone una etiqueta y se encuentra tanto filtrando como desde `Ctrl+K`.

### Fase 12 — Pulido
- Revisar las animaciones de crear, eliminar, completar y reordenar.
- Agregar estados vacíos ("No hay notas para hoy").
- Mostrar mensajes claros cuando falla la sincronización.

### Fase 13 — Instalador descargable y actualizaciones automáticas
- Publicar la app *self-contained*, para que no haga falta instalar .NET aparte.
- Crear el instalador `Setup.exe` con **Velopack**:
  - Instala sin pedir permisos de administrador y crea el acceso directo en el menú Inicio y en el escritorio.
  - Al desinstalar, también quita el inicio automático con Windows.
- En el **primer arranque**:
  - Pedir la cadena de conexión de Neon y guardarla cifrada en el equipo (DPAPI), no dentro del instalador.
  - Mostrar la casilla "Iniciar con Windows" (activada por defecto).
- **Actualizaciones automáticas**:
  - Publicar cada versión nueva en GitHub Releases.
  - La app revisa al iniciar y cada pocas horas si hay versión nueva, la descarga en segundo plano y avisa "Hay una actualización lista. Reiniciar".
  - Si no se reinicia, se aplica la próxima vez que se abra la app.
  - Los datos locales (SQLite) y la configuración se conservan.
- Agregar un ícono propio para la app, el instalador y la bandeja.

**Listo cuando:** en una computadora limpia, el `Setup.exe` instala la app, esta arranca con Windows, el atajo global la abre, los avisos funcionan y, al publicar una versión nueva, la app se actualiza sola sin perder datos.
