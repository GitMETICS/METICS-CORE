(function () {
    'use strict';

    const navbar = document.querySelector('.metics-tabs-navbar');
    if (!navbar) return;

    function closeOtherDropdowns(activeDropdown) {
        navbar.querySelectorAll('.dropdown').forEach(function (dropdown) {
            if (dropdown === activeDropdown) return;

            const toggle = dropdown.querySelector('[data-bs-toggle="dropdown"]');
            if (!toggle) return;

            if (dropdown.classList.contains('show') && window.bootstrap && window.bootstrap.Dropdown) {
                const instance = window.bootstrap.Dropdown.getInstance(toggle);
                if (instance) instance.hide();
            }

            if (dropdown.matches(':focus-within')) {
                const focusedElement = dropdown.querySelector(':focus');
                if (focusedElement) focusedElement.blur();
            }
        });
    }

    navbar.addEventListener('show.bs.dropdown', function (event) {
        closeOtherDropdowns(event.target.closest('.dropdown'));
    });

    navbar.addEventListener('pointerover', function (event) {
        if (!window.matchMedia('(hover: hover) and (pointer: fine)').matches) return;

        const activeDropdown = event.target.closest('.dropdown');
        if (activeDropdown && navbar.contains(activeDropdown)) {
            closeOtherDropdowns(activeDropdown);
        }
    });
})();
