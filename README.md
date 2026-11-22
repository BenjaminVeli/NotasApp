# Mi espacio — notas, reuniones y código

App de escritorio para Windows, de uso personal, para:

- **Notas**: apuntar cosas para no olvidarlas.
- **Reuniones**: agendarlas con fecha y hora y recibir un aviso de Windows antes de que empiecen.
- **Código**: guardar fragmentos de código ordenados por categoría (SQL, CSS, C#…), con un editor como el de VS Code.

Funciona sin internet: todo se guarda en el equipo. Tema oscuro estilo macOS/iOS.

---

## Instalación

### Versión portable (recomendada por ahora)

1. Descarga o copia `NotasApp-portable.zip` y descomprímelo en una carpeta fija, por ejemplo `C:\Users\<tu usuario>\Apps\NotasApp`.
2. Abre `NotasApp.exe`.

No hace falta instalar .NET (va incluido). Solo requiere **WebView2**, que ya viene con Windows 10 y 11.

> La primera vez, Windows SmartScreen puede advertir que la app no es conocida (el `.exe` no está firmado). Elige **Más información → Ejecutar de todas formas**.

Para generar el `.zip` desde el código:

```
dotnet publish src/NotasApp -c Release -r win-x64 --self-contained -o publish/NotasApp-portable
```

### Desde el código (para desarrollar)

Requiere el **SDK de .NET 10**.

```
git clone https://github.com/BenjaminVeli/NotasApp.git
cd NotasApp
dotnet run --project src/NotasApp
```

> Si la app ya está abierta (aunque sea en la bandeja), ciérrala con **clic derecho en su ícono → Salir** antes de volver a ejecutarla: si no, solo se muestra la ventana de la versión vieja y la compilación falla porque el `.exe` está en uso.

---

## Uso

La ventana tiene un **sidebar** a la izquierda (Buscar, Notas, Reuniones, Código y sus categorías, y abajo el estado de conexión y la configuración ⚙) y el contenido a la derecha.

### Notas

- **Nueva** (o `Ctrl+N`): título, texto y día al que pertenece la nota (Hoy, Mañana u Otro día).
- **Clic en una nota** para editarla. `Ctrl+Enter` guarda, `Esc` cancela.
- **Círculo** a la izquierda: marcar como completada.
- **Estrella** ☆: favorita. **Papelera**: eliminar (pide confirmación).
- **Arrastrar** desde el ícono ⋮⋮ de la derecha para cambiar el orden.

### Reuniones

- **Nueva** (o `Ctrl+N`): título, nota, día y **hora** con un selector tipo rueda (como en el iPhone). La rueda se mueve con la rueda del mouse, arrastrando, con clic o con las flechas.
- Se ordenan solas por hora dentro de cada día; después se pueden reordenar arrastrando.
- La hora se ve **azul**; **naranja** si ya pasó y la reunión sigue pendiente; **gris** si está completada.

### Filtros (Notas y Reuniones)

Se combinan entre sí:

- **Fecha**: Todo, Hoy, Mañana, Semana, o **Elegir día** (calendario: un clic elige un día, un segundo clic arma un rango). Los días con pendientes tienen un punto.
- **Estado**: Todas, Pendientes o Completadas.
- **Tipo**: «Incluir reuniones» / «Incluir notas» muestra ambas cosas juntas, agrupadas por día.
- **Restablecer** vuelve a los filtros por defecto.

### Código

- **Categorías**: el botón **+** junto a «Categorías» crea una (nombre, lenguaje para el resaltado y extensión). El lápiz junto al título la edita o elimina (con sus archivos). Se reordenan arrastrándolas en el sidebar.
- **Nuevo archivo** (o `Ctrl+N`): si no escribes extensión se agrega la de la categoría (`consulta` → `consulta.sql`). Doble clic en un archivo para **renombrarlo**.
- **Editor**: resaltado según el lenguaje de la categoría.
  - **Se guarda solo** un segundo después de dejar de escribir («Guardado» en verde). `Ctrl+S` guarda al instante.
  - **Copiar** (o `Ctrl+Mayús+C`) copia todo el archivo.
  - **Exportar** (o `Ctrl+E`) lo guarda como archivo donde elijas (con su extensión).

### Búsqueda global

`Ctrl+K` o **Buscar** en el sidebar. Busca en títulos, contenido y código de notas, reuniones y archivos a la vez, sin importar mayúsculas ni tildes («reunion» encuentra «reunión»). Con `↑ ↓` eliges y con `Enter` abres: la nota o reunión queda resaltada en su lista; el archivo se abre en el editor con la palabra seleccionada.

### Segundo plano y avisos

- **Cerrar la ventana no cierra la app**: queda en la **bandeja** (junto al reloj) para avisarte de tus reuniones. Para cerrarla del todo: **clic derecho en su ícono → Salir**.
- **Avisos de reuniones**: una notificación de Windows unos minutos antes (5, 10, 15 o 30, se elige en Configuración) y otra a la hora. Desde el aviso puedes elegir **Recordar en 5 min**, **Recordar en 10 min** o **Descartar**; al hacer clic, se abre la app en esa reunión.
- **Iniciar con Windows** (Configuración): la app arranca oculta en la bandeja al encender la computadora.

---

## Atajos de teclado

La lista completa también se ve dentro de la app con `Ctrl+/` o `F1`.

| Atajo | Acción |
|---|---|
| `Ctrl+Alt+N` | Mostrar u ocultar la app **desde cualquier programa** (se cambia en Configuración) |
| `Ctrl+K` | Búsqueda global |
| `Ctrl+N` | Nuevo elemento en la sección actual |
| `Ctrl+1` / `Ctrl+2` / `Ctrl+3` | Ir a Notas / Reuniones / Código |
| `Ctrl+/` o `F1` | Lista de atajos |
| `Esc` | Cerrar ventana, búsqueda o edición |
| `↑` `↓` | Elegir nota o reunión (en Código: archivo anterior/siguiente) |
| `Enter` | Editar la nota o reunión elegida |
| `Ctrl+Enter` | Marcar como completada |
| `Ctrl+D` | Marcar o quitar de favoritos |
| `Supr` | Eliminar (pide confirmación: `Supr` otra vez o `Enter`) |
| `Ctrl+S` | Guardar archivo de código |
| `Ctrl+E` | Exportar el archivo actual |
| `Ctrl+Mayús+C` | Copiar todo el archivo actual |

Dentro del editor de código, `Ctrl+D`, `Ctrl+/` y `Supr` hacen lo habitual de un editor (multicursor, comentar, borrar).

---

## Dónde se guardan los datos

Todo está en `%LOCALAPPDATA%\NotasApp\` en cada computadora:

| Archivo | Contenido |
|---|---|
| `notas.db` | Base de datos SQLite con notas, reuniones, categorías y archivos |
| `config.json` | Configuración (inicio con Windows, minutos del aviso, atajo global) |
| `avisos.json` | Avisos ya mostrados, para no repetirlos |

Para hacer una copia de seguridad basta con copiar esa carpeta con la app cerrada.

### Sincronización con Neon (opcional, en desarrollo)

Sin configurar nada, la app trabaja solo con el equipo e indica **Sin conexión** abajo en el sidebar. Para conectarla a una base PostgreSQL en [Neon](https://neon.tech), en desarrollo:

```
dotnet user-secrets set "ConnectionStrings:Neon" "<cadena de conexión>" --project src/NotasApp
```

Al iniciar, la app crea las tablas en Neon y el indicador pasa a **En línea**. La sincronización de datos entre equipos todavía no está hecha (fase 7).

---

## Para desarrolladores

**Stack**: WPF (.NET 10) + Blazor en `BlazorWebView` (WebView2), EF Core con SQLite (local) y Npgsql (Neon), BlazorMonaco, SortableJS, notificaciones de Windows (`CommunityToolkit.WinUI.Notifications`) y bandeja (`H.NotifyIcon.Wpf`).

```
src/NotasApp/
├─ App.xaml.cs          Arranque: servicios, base de datos, bandeja, instancia única, atajo global, avisos
├─ Data/                Modelos, contextos de EF Core (SQLite y Neon) y migraciones
├─ Services/            Lógica: notas/reuniones, archivos, búsqueda (FTS5), avisos, atajos, configuración
├─ Pages/               Notas, Reuniones, Código, Configuración
├─ Shared/              Layout, lista con filtros, editores, calendario, selector de hora, búsqueda…
└─ wwwroot/             HTML, CSS (tema), JS (arrastrar, rueda, atajos) y librerías locales
```

Migraciones (con la herramienta local `dotnet-ef`):

```
dotnet tool restore
dotnet ef migrations add <Nombre> -c LocalDbContext -o Data/Migraciones/Local -p src/NotasApp
dotnet ef migrations add <Nombre> -c CloudDbContext -o Data/Migraciones/Nube -p src/NotasApp
```

El plan completo y el diseño están en [`PLAN_NotasApp.md`](PLAN_NotasApp.md) y [`notas_app_mockup_dark.html`](notas_app_mockup_dark.html).

### Estado

| Fase | Estado |
|---|---|
| 0–6 · Proyecto, tema, notas, reuniones, filtros, segundo plano y avisos, código | ✅ |
| 7 · Modo offline y sincronización con Neon | Pendiente |
| 8 · Búsqueda global | ✅ |
| 9 · Atajos de teclado | ✅ |
| 10 · Exportar e importar (categorías, todo en `.zip`, importar) | Parcial: exportar un archivo |
| 11 · Favoritos y etiquetas | Parcial: estrella de favorito |
| 12 · Pulido | Pendiente |
| 13 · Instalador y actualizaciones automáticas | Pendiente |
