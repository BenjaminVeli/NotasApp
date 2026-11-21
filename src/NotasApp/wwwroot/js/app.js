window.notas = {
    /** Activa arrastrar y ordenar en un contenedor de tarjetas (.cd). */
    sortable(el, dotnet) {
        const s = Sortable.create(el, {
            handle: '.grip',
            draggable: '.cd',
            animation: 180,
            easing: 'cubic-bezier(0.32, 0.72, 0, 1)',
            ghostClass: 'arrastrando',
            // El WebView2 de WPF (modo composición) no entrega el arrastre nativo de HTML5
            // con el mouse real: se arrastra con eventos de puntero.
            forceFallback: true,
            fallbackClass: 'arrastre-flotante',
            fallbackTolerance: 3,
            onEnd(e) {
                if (e.oldIndex === e.newIndex) return;
                // Devuelve el nodo a su lugar: Blazor es quien reordena el DOM.
                e.item.remove();
                e.from.insertBefore(e.item, e.from.children[e.oldIndex] ?? null);
                dotnet.invokeMethodAsync('Mover', e.oldDraggableIndex, e.newDraggableIndex);
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

    /** Desplaza la vista hasta un elemento (p. ej. una reunión abierta desde un aviso). */
    verElemento(id) {
        requestAnimationFrame(() => document.getElementById(id)?.scrollIntoView({ behavior: 'smooth', block: 'center' }));
    }
};
