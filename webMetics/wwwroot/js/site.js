// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// URL del archivo de idioma de DataTables. Pinned a la rama 1.x para que coincida
// con la versión de DataTables cargada en _Layout (los nombres de clave del archivo
// 2.x no se mapean sobre el core 1.x y dejarían los textos en inglés).
var METICS_DT_LANG = '//cdn.datatables.net/plug-ins/1.13.8/i18n/es-ES.json';

// Helper compartido para las tablas de listado de administración.
// Reemplaza el bloque init + render*Pagination + page-size + tooltips que antes
// se copiaba en cada vista.
var MeticsTable = (function () {
    function initTooltips(root) {
        if (!window.bootstrap || !window.bootstrap.Tooltip) return;
        var $root = root ? $(root) : $(document);
        $root.find('[data-bs-toggle="tooltip"]').each(function () {
            if (!bootstrap.Tooltip.getInstance(this)) {
                new bootstrap.Tooltip(this);
            }
        });
    }

    // Construye un botón/elemento de paginación con la clase compartida .page-btn.
    function makePageButton(label, targetPageIndex, table, isCurrent) {
        if (isCurrent) {
            var span = document.createElement('span');
            span.className = 'page-btn current';
            span.textContent = label;
            return span;
        }

        var link = document.createElement('a');
        link.href = '#';
        link.className = 'page-btn';
        link.textContent = label;
        link.addEventListener('click', function (event) {
            event.preventDefault();
            table.page(targetPageIndex).draw(false);
        });
        return link;
    }

    function renderPagination(table, container) {
        if (!container) return;

        container.innerHTML = '';

        var pageInfo = table.page.info();
        var pageCount = Math.ceil(pageInfo.recordsDisplay / pageInfo.length);
        var currentPage = pageInfo.page + 1;

        if (pageCount <= 1) return;

        if (currentPage > 1) {
            container.appendChild(makePageButton('«', currentPage - 2, table, false));
        }

        for (var i = 1; i <= pageCount; i++) {
            container.appendChild(makePageButton(String(i), i - 1, table, i === currentPage));
        }

        if (currentPage < pageCount) {
            container.appendChild(makePageButton('»', currentPage, table, false));
        }
    }

    // options: { tableId, pageSizeId, paginationId, searching, searchInputId }
    function init(options) {
        var tableElement = document.getElementById(options.tableId);
        if (!tableElement) return null;

        var table = new DataTable('#' + options.tableId, {
            language: { url: METICS_DT_LANG },
            searching: options.searching === true,
            paging: true,
            pageLength: options.pageLength || 5,
            lengthMenu: [5, 10, 25, 50],
            info: false,
            lengthChange: false,
            dom: 't',
            pagingType: 'simple_numbers',
            drawCallback: function () {
                initTooltips(this.api().table().node());
            }
        });

        var pagination = options.paginationId ? document.getElementById(options.paginationId) : null;
        if (pagination) {
            var draw = function () { renderPagination(table, pagination); };
            table.on('draw', draw);
            draw();
        }

        var pageSize = options.pageSizeId ? document.getElementById(options.pageSizeId) : null;
        if (pageSize) {
            pageSize.addEventListener('change', function () {
                table.page.len(parseInt(this.value, 10)).draw();
            });
        }

        var searchInput = options.searchInputId ? document.getElementById(options.searchInputId) : null;
        if (searchInput) {
            var runSearch = function () { table.search(searchInput.value.trim()).draw(); };
            searchInput.addEventListener('input', runSearch);
            searchInput.addEventListener('keypress', function (event) {
                if (event.key === 'Enter') {
                    event.preventDefault();
                    runSearch();
                }
            });
        }

        initTooltips(tableElement);
        return table;
    }

    return { init: init, initTooltips: initTooltips };
})();
