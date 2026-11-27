(function () {
    'use strict';

    const header = document.querySelector('.metics-unified-header');
    if (!header) return;

    // Se expande al volver por debajo de este punto.
    const expandThreshold = 24;
    // Margen extra para no decidir al límite mientras el encabezado termina su transición.
    const safetyMargin = 4;
    let framePending = false;

    function cssPixels(name, fallback) {
        const value = parseFloat(getComputedStyle(header).getPropertyValue(name));
        return Number.isFinite(value) ? value : fallback;
    }

    // Cuántos píxeles pierde el encabezado al compactarse.
    function shrinkAmount() {
        return Math.max(0,
            cssPixels('--metics-header-expanded-height', 98) - cssPixels('--metics-header-compact-height', 78));
    }

    function updateHeaderState() {
        framePending = false;
        const isCompact = header.classList.contains('is-scrolled');
        const scrollY = window.scrollY;

        if (isCompact) {
            if (scrollY <= expandThreshold) {
                header.classList.remove('is-scrolled');
            }
            return;
        }

        // Al compactar, la página se acorta y el navegador puede mover el scroll esos mismos
        // píxeles (o limitarlo si ya no hay espacio). Sin margen suficiente, el encabezado se
        // expande y compacta en bucle y la pantalla "tiembla". Por eso:
        //  1. solo se compacta pasado el punto de expansión + lo que encoge el encabezado, y
        //  2. solo si, tras compactar, la página aún tiene scroll por encima del punto de expansión.
        const shrink = shrinkAmount();
        const compactThreshold = expandThreshold + shrink + safetyMargin;
        const maxScroll = document.documentElement.scrollHeight - window.innerHeight;
        if (scrollY > compactThreshold && maxScroll - shrink > expandThreshold + safetyMargin) {
            header.classList.add('is-scrolled');
        }
    }

    function onScroll() {
        if (framePending) return;
        framePending = true;
        window.requestAnimationFrame(updateHeaderState);
    }

    window.addEventListener('scroll', onScroll, { passive: true });
    updateHeaderState();
})();
