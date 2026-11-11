(function () {
    'use strict';

    const header = document.querySelector('.metics-unified-header');
    if (!header) return;

    const scrollThreshold = 24;
    let framePending = false;

    function updateHeaderState() {
        header.classList.toggle('is-scrolled', window.scrollY > scrollThreshold);
        framePending = false;
    }

    function onScroll() {
        if (framePending) return;
        framePending = true;
        window.requestAnimationFrame(updateHeaderState);
    }

    window.addEventListener('scroll', onScroll, { passive: true });
    updateHeaderState();
})();
