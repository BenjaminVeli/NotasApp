window.notas = {
    /**
     * Activa arrastrar y ordenar en un contenedor. Por defecto: tarjetas (.cd) con agarre (.grip)
     * y avisa a .NET con el método "Mover"; `opciones` permite cambiar los tres.
     * Con `revertir: false` no devuelve el nodo a su lugar: .NET debe volver a crear la lista
     * (cambiando su @key), porque Blazor no reubica bien componentes NavLink movidos a mano.
     */
    sortable(el, dotnet, opciones = {}) {
        const { handle = '.grip', draggable = '.cd', metodo = 'Mover', revertir = true } = opciones;
        const s = Sortable.create(el, {
            handle: handle || undefined,   // null = se arrastra desde cualquier parte del elemento
            draggable,
            animation: 180,
            easing: 'cubic-bezier(0.32, 0.72, 0, 1)',
            ghostClass: 'arrastrando',
            // El WebView2 de WPF (modo composición) no entrega el arrastre nativo de HTML5
            // con el mouse real: se arrastra con eventos de puntero.
            forceFallback: true,
            fallbackClass: 'arrastre-flotante',
            fallbackOnBody: true,   // la copia flotante va a <body>, no dentro de la lista que Blazor administra
            fallbackTolerance: 3,
            onEnd(e) {
                if (e.oldIndex === e.newIndex) return;
                if (revertir) {
                    // Devuelve el nodo a su lugar: Blazor es quien reordena el DOM.
                    e.item.remove();
                    e.from.insertBefore(e.item, e.from.children[e.oldIndex] ?? null);
                }
                dotnet.invokeMethodAsync(metodo, e.oldDraggableIndex, e.newDraggableIndex);
            }
        });
        return { destroy: () => s.destroy() };
    },

    /**
     * Columna de selector tipo rueda (estilo iOS). Avisa a .NET con el índice elegido.
     * Se mueve con la rueda del mouse (un paso por muesca), arrastrando, con clic y con flechas.
     */
    rueda(el, dotnet, metodo, indice) {
        const H = 28;
        const items = [...el.querySelectorAll('.rueda-item')];
        const n = items.length;
        let actual = indice, timer = 0, arrastre = null;

        const pintar = () => {
            const c = el.scrollTop / H;
            items.forEach((it, i) => {
                const d = Math.min(Math.abs(i - c), 3);
                it.style.opacity = (1 - d * 0.3).toFixed(2);
                it.style.transform = `perspective(240px) rotateX(${((c - i) * 20).toFixed(1)}deg) scale(${(1 - d * 0.05).toFixed(3)})`;
                it.classList.toggle('sel', i === Math.round(c));
            });
        };

        const ir = (i, suave = true) => {
            i = Math.max(0, Math.min(n - 1, i));
            el.scrollTo({ top: i * H, behavior: suave ? 'smooth' : 'instant' });
            if (i !== actual) {
                actual = i;
                dotnet.invokeMethodAsync(metodo, i);
            }
        };

        const alDesplazar = () => {
            requestAnimationFrame(pintar);
            clearTimeout(timer);
            if (!arrastre) timer = setTimeout(() => ir(Math.round(el.scrollTop / H)), 110);
        };

        const alRueda = e => {
            e.preventDefault();
            ir(actual + Math.sign(e.deltaY));
        };

        const alPresionar = e => {
            arrastre = { y: e.clientY, top: el.scrollTop, t: performance.now(), v: 0, movido: false };
            el.setPointerCapture(e.pointerId);
        };

        const alMover = e => {
            if (!arrastre) return;
            const dy = e.clientY - arrastre.y;
            if (Math.abs(dy) > 3) arrastre.movido = true;
            const ahora = performance.now();
            const nuevo = arrastre.top - dy;
            arrastre.v = (nuevo - el.scrollTop) / Math.max(1, ahora - arrastre.t);
            arrastre.t = ahora;
            el.scrollTop = nuevo;
        };

        const alSoltar = e => {
            if (!arrastre) return;
            const { movido, v } = arrastre;
            arrastre = null;
            if (!movido) {
                // Clic: con el puntero capturado el target es la columna, así que se calcula
                // la fila por posición (hay 2 filas de relleno arriba).
                const y = e.clientY - el.getBoundingClientRect().top + el.scrollTop;
                ir(Math.floor(y / H) - 2);
                return;
            }
            // Inercia simple: proyecta la velocidad del arrastre.
            ir(Math.round((el.scrollTop + v * 120) / H));
        };

        const alTecla = e => {
            if (e.key === 'ArrowUp') { e.preventDefault(); ir(actual - 1); }
            else if (e.key === 'ArrowDown') { e.preventDefault(); ir(actual + 1); }
        };

        el.scrollTop = actual * H;
        pintar();
        el.addEventListener('scroll', alDesplazar);
        el.addEventListener('wheel', alRueda, { passive: false });
        el.addEventListener('pointerdown', alPresionar);
        el.addEventListener('pointermove', alMover);
        el.addEventListener('pointerup', alSoltar);
        el.addEventListener('pointercancel', alSoltar);
        el.addEventListener('keydown', alTecla);

        return {
            fijar: i => ir(i, false),
            destroy() {
                clearTimeout(timer);
                el.removeEventListener('scroll', alDesplazar);
                el.removeEventListener('wheel', alRueda);
                el.removeEventListener('pointerdown', alPresionar);
                el.removeEventListener('pointermove', alMover);
                el.removeEventListener('pointerup', alSoltar);
                el.removeEventListener('pointercancel', alSoltar);
                el.removeEventListener('keydown', alTecla);
            }
        };
    },

    /**
     * Prepara un editor Monaco: tema con los colores del mockup y atajos Ctrl+S / Ctrl+Shift+C.
     * Se usa addAction (atado a este editor) y no addCommand, que en Monaco es global y
     * seguiría llamando a un editor ya destruido cuando se vuelve a crear.
     */
    prepararEditor(editorId, dotnet) {
        if (!window.notas.temaDefinido) {
            monaco.editor.defineTheme('notas-oscuro', {
                base: 'vs-dark',
                inherit: true,
                rules: [
                    { token: 'keyword', foreground: '569cd6' },
                    { token: 'string', foreground: 'ce9178' },
                    { token: 'string.sql', foreground: 'ce9178' }, // vs-dark la pinta de rojo puro
                    { token: 'number', foreground: 'b5cea8' },
                    { token: 'comment', foreground: '6a9955' },
                ],
                colors: {
                    'editor.background': '#1e1e1e',
                    'editorLineNumber.foreground': '#5a5a5a',
                    'editorLineNumber.activeForeground': '#c6c6c6',
                },
            });
            window.notas.temaDefinido = true;
        }
        monaco.editor.setTheme('notas-oscuro');

        const editor = window.blazorMonaco.editor.getEditor(editorId);
        const { KeyMod, KeyCode } = monaco;
        editor.addAction({
            id: 'notas.guardar', label: 'Guardar archivo',
            keybindings: [KeyMod.CtrlCmd | KeyCode.KeyS],
            run: () => dotnet.invokeMethodAsync('AtajoGuardar'),
        });
        editor.addAction({
            id: 'notas.copiar', label: 'Copiar todo el archivo',
            keybindings: [KeyMod.CtrlCmd | KeyMod.Shift | KeyCode.KeyC],
            run: () => dotnet.invokeMethodAsync('AtajoCopiar'),
        });
    },

    /**
     * Atajos de toda la app. `atajos` es { "ctrl+k": "MetodoNet", ... }. Se escuchan en fase de
     * captura para ganarle a Monaco (que usa Ctrl+K como inicio de combinaciones).
     */
    atajosGlobales(dotnet, atajos) {
        document.addEventListener('keydown', e => {
            if (e.repeat) return;
            const combo = [e.ctrlKey && 'ctrl', e.shiftKey && 'shift', e.altKey && 'alt', e.key.toLowerCase()]
                .filter(Boolean).join('+');
            const metodo = atajos[combo];
            if (!metodo) return;
            e.preventDefault();
            e.stopPropagation();
            dotnet.invokeMethodAsync(metodo);
        }, true);
    },

    /** Desplaza lo mínimo para que un elemento de una lista con scroll quede visible. */
    verEnLista(id) {
        document.getElementById(id)?.scrollIntoView({ block: 'nearest' });
    },

    /** Selecciona en el editor la primera aparición de lo buscado (sin distinguir mayúsculas). */
    buscarEnEditor(editorId, texto) {
        const editor = window.blazorMonaco.editor.getEditor(editorId, true);
        const palabras = texto.match(/[\p{L}\p{N}_]+/gu);
        if (!editor || !palabras) return;
        const modelo = editor.getModel();
        // Primero la frase completa; si no aparece, la primera palabra.
        const coincidencias = modelo.findMatches(texto.trim(), false, false, false, null, false);
        const encontrada = coincidencias[0] ?? modelo.findMatches(palabras[0], false, false, false, null, false)[0];
        if (!encontrada) return;
        // La búsqueda es por prefijo ("presup"): se selecciona la palabra completa ("presupuesto").
        let rango = encontrada.range;
        const palabra = modelo.getWordAtPosition(rango.getStartPosition());
        if (palabra && rango.startLineNumber === rango.endLineNumber && palabra.endColumn > rango.endColumn)
            rango = new monaco.Range(rango.startLineNumber, rango.startColumn, rango.endLineNumber, palabra.endColumn);
        editor.setSelection(rango);
        editor.revealRangeInCenter(rango);
        editor.focus();
    },

    /** Desplaza la vista hasta un elemento (p. ej. una reunión abierta desde un aviso). */
    verElemento(id) {
        requestAnimationFrame(() => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'center' }));
    }
};
