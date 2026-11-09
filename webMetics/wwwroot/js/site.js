// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Muestra los errores de campos obligatorios dentro de los modales METICS,
// evitando el aviso nativo del navegador y conservando la validación del servidor.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('form.inline-required-validation').forEach(function (form) {
        form.noValidate = true;
        var attemptedSubmit = false;

        function getMessageElement(field) {
            if (!field.name) return null;
            return Array.from(form.querySelectorAll('[data-valmsg-for]')).find(function (message) {
                return message.getAttribute('data-valmsg-for') === field.name;
            }) || null;
        }

        function getFieldProxy(field) {
            var dateWrapper = field.closest('.create-group-custom-date');
            if (dateWrapper) return { control: dateWrapper.querySelector('.create-group-date-text'), wrapper: dateWrapper };

            var selectWrapper = field.closest('.create-group-custom-select');
            if (selectWrapper) return { control: selectWrapper.querySelector('.create-group-select-trigger'), wrapper: selectWrapper };

            return { control: field, wrapper: null };
        }

        function validateField(field) {
            if (!field.required || field.disabled) return true;

            var empty = field.type === 'checkbox' || field.type === 'radio'
                ? !field.checked
                : !String(field.value || '').trim();
            var valid = !empty && field.validity.valid;
            var message = getMessageElement(field);
            var proxy = getFieldProxy(field);

            field.classList.toggle('input-validation-error', !valid);
            proxy.control.classList.toggle('input-validation-error', !valid);
            proxy.control.setAttribute('aria-invalid', valid ? 'false' : 'true');
            if (proxy.wrapper) proxy.wrapper.classList.toggle('has-validation-error', !valid);

            if (message) {
                if (valid) {
                    message.textContent = '';
                    message.classList.remove('field-validation-error');
                    message.classList.add('field-validation-valid');
                } else {
                    var label = Array.from(form.querySelectorAll('label')).find(function (item) {
                        return item.htmlFor === field.id;
                    });
                    var labelText = label ? label.textContent.replace('*', '').trim() : 'Este campo';
                    var errorText = empty
                        ? labelText + ' es obligatorio.'
                        : field.validity.customError
                            ? field.validationMessage
                            : field.validity.rangeUnderflow || field.validity.rangeOverflow
                                ? 'El valor debe estar entre ' + (field.min || 'el mínimo permitido') + ' y ' + (field.max || 'el máximo permitido') + '.'
                                : field.type === 'email'
                                    ? 'Ingresa un correo electrónico válido.'
                                    : 'Revisa este valor.';

                    message.textContent = errorText;
                    message.classList.remove('field-validation-valid');
                    message.classList.add('field-validation-error');
                }
            }

            return valid;
        }

        form.addEventListener('submit', function (event) {
            attemptedSubmit = true;
            var requiredFields = Array.from(form.querySelectorAll('[required]')).filter(function (field) {
                return !field.disabled;
            });
            var firstInvalid = null;

            requiredFields.forEach(function (field) {
                if (!validateField(field) && !firstInvalid) firstInvalid = field;
            });

            var requiredGroups = Array.from(form.querySelectorAll('[data-required-group]'));
            var firstInvalidGroupControl = null;
            requiredGroups.forEach(function (group) {
                var groupName = group.getAttribute('data-required-group');
                var choices = Array.from(group.querySelectorAll('[name="' + groupName + '"]'));
                var selected = choices.some(function (choice) { return choice.checked; });
                var message = Array.from(group.querySelectorAll('[data-valmsg-for]')).find(function (item) {
                    return item.getAttribute('data-valmsg-for') === groupName;
                });
                group.classList.toggle('has-validation-error', !selected);
                if (message) {
                    message.textContent = selected ? '' : 'Debe seleccionar al menos una competencia.';
                    message.classList.toggle('field-validation-error', !selected);
                    message.classList.toggle('field-validation-valid', selected);
                }
                choices.forEach(function (choice) { choice.setAttribute('aria-invalid', selected ? 'false' : 'true'); });
                if (!selected && !firstInvalidGroupControl) firstInvalidGroupControl = choices[0] || null;
            });

            if (firstInvalid || firstInvalidGroupControl) {
                event.preventDefault();
                var firstInvalidField = firstInvalid || firstInvalidGroupControl;
                var fieldProxy = getFieldProxy(firstInvalidField);
                (fieldProxy.control || firstInvalidField).focus();
            }
        });

        form.addEventListener('input', function (event) {
            if (attemptedSubmit && event.target.matches('[required]')) validateField(event.target);
        });
        form.addEventListener('change', function (event) {
            if (attemptedSubmit && event.target.matches('[required]')) validateField(event.target);
        });

        form.addEventListener('change', function (event) {
            var group = event.target.closest('[data-required-group]');
            if (!group) return;

            var groupName = group.getAttribute('data-required-group');
            var choices = Array.from(group.querySelectorAll('[name="' + groupName + '"]'));
            var selected = choices.some(function (choice) { return choice.checked; });
            var message = Array.from(group.querySelectorAll('[data-valmsg-for]')).find(function (item) {
                return item.getAttribute('data-valmsg-for') === groupName;
            });
            group.classList.toggle('has-validation-error', !selected && attemptedSubmit);
            if (message && (selected || attemptedSubmit)) {
                message.textContent = selected ? '' : 'Debe seleccionar al menos una competencia.';
                message.classList.toggle('field-validation-error', !selected);
                message.classList.toggle('field-validation-valid', selected);
            }
            choices.forEach(function (choice) { choice.setAttribute('aria-invalid', selected ? 'false' : 'true'); });
        });
    });
});

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
    function makePageButton(label, targetPageIndex, table, isCurrent, ariaLabel, isDisabled) {
        var isArrow = label === '‹' || label === '›';
        var appendLabel = function (element) {
            if (isArrow) {
                var icon = document.createElement('i');
                icon.className = label === '‹' ? 'bi bi-chevron-left' : 'bi bi-chevron-right';
                icon.setAttribute('aria-hidden', 'true');
                element.classList.add('page-btn-arrow');
                element.appendChild(icon);
            } else {
                element.textContent = label;
            }
        };

        if (isDisabled) {
            var disabledButton = document.createElement('button');
            disabledButton.type = 'button';
            disabledButton.className = 'page-btn disabled';
            disabledButton.disabled = true;
            appendLabel(disabledButton);
            if (ariaLabel) disabledButton.setAttribute('aria-label', ariaLabel);
            return disabledButton;
        }

        if (isCurrent) {
            var span = document.createElement('span');
            span.className = 'page-btn current';
            span.textContent = label;
            span.setAttribute('aria-current', 'page');
            span.setAttribute('aria-label', 'Página ' + label + ', actual');
            return span;
        }

        var link = document.createElement('a');
        link.href = '#';
        link.className = 'page-btn';
        appendLabel(link);
        if (ariaLabel) link.setAttribute('aria-label', ariaLabel);
        link.addEventListener('click', function (event) {
            event.preventDefault();
            table.page(targetPageIndex).draw(false);
        });
        return link;
    }

    function makePaginationEllipsis() {
        var span = document.createElement('span');
        span.className = 'page-ellipsis';
        span.textContent = '…';
        span.setAttribute('aria-label', 'Páginas omitidas');
        return span;
    }

    function renderPagination(table, container) {
        if (!container) return;

        container.innerHTML = '';

        var pageInfo = table.page.info();
        var pageCount = Math.ceil(pageInfo.recordsDisplay / pageInfo.length);
        var currentPage = pageInfo.page + 1;

        if (pageCount <= 1) return;

        container.appendChild(makePageButton('‹', currentPage - 2, table, false, 'Página anterior', currentPage === 1));

        if (pageCount <= 5) {
            for (var page = 1; page <= pageCount; page++) {
                container.appendChild(makePageButton(String(page), page - 1, table, page === currentPage, 'Ir a la página ' + page));
            }
        } else {
            container.appendChild(makePageButton('1', 0, table, currentPage === 1, 'Ir a la página 1'));

            var startPage;
            var endPage;
            if (currentPage <= 3) {
                startPage = 2;
                endPage = 4;
            } else if (currentPage >= pageCount - 2) {
                startPage = pageCount - 3;
                endPage = pageCount - 1;
            } else {
                startPage = currentPage - 1;
                endPage = currentPage + 1;
            }

            if (startPage > 2) {
                container.appendChild(makePaginationEllipsis());
            }

            for (var i = startPage; i <= endPage; i++) {
                container.appendChild(makePageButton(String(i), i - 1, table, i === currentPage, 'Ir a la página ' + i));
            }

            if (endPage < pageCount - 1) {
                container.appendChild(makePaginationEllipsis());
            }

            container.appendChild(makePageButton(String(pageCount), pageCount - 1, table, currentPage === pageCount, 'Ir a la página ' + pageCount));
        }

        container.appendChild(makePageButton('›', currentPage, table, false, 'Página siguiente', currentPage === pageCount));
    }

    // options: { tableId, pageSizeId, paginationId, searching, searchInputId }
    function init(options) {
        var tableElement = document.getElementById(options.tableId);
        if (!tableElement) return null;

        var initialPageLength = options.pageLength || 50;
        var table = new DataTable('#' + options.tableId, {
            language: { url: METICS_DT_LANG },
            searching: options.searching === true,
            paging: true,
            pageLength: initialPageLength,
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
            pageSize.value = String(initialPageLength);
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

    // Inicializa una tabla cuya búsqueda, ordenamiento y paginación se resuelven en el servidor.
    function initServer(options) {
        var tableElement = document.getElementById(options.tableId);
        if (!tableElement) return null;

        var table = new DataTable('#' + options.tableId, {
            language: { url: METICS_DT_LANG },
            processing: true,
            serverSide: true,
            searching: true,
            paging: true,
            pageLength: options.pageLength || 50,
            lengthMenu: [5, 10, 25, 50],
            info: false,
            lengthChange: false,
            dom: 't',
            pagingType: 'simple_numbers',
            search: { search: options.initialSearch || '' },
            order: options.order || [[1, 'asc']],
            ajax: {
                url: options.ajaxUrl,
                type: 'GET'
            },
            columns: options.columns,
            columnDefs: options.columnDefs || [],
            drawCallback: function () {
                var api = this.api();
                initTooltips(api.table().node());
                if (typeof options.onDraw === 'function') {
                    options.onDraw(api);
                }
            }
        });

        var pagination = options.paginationId ? document.getElementById(options.paginationId) : null;
        if (pagination) {
            var draw = function () { renderPagination(table, pagination); };
            table.on('draw', draw);
        }

        var pageSize = options.pageSizeId ? document.getElementById(options.pageSizeId) : null;
        if (pageSize) {
            pageSize.value = String(options.pageLength || 50);
            pageSize.addEventListener('change', function () {
                table.page.len(parseInt(this.value, 10)).draw();
            });
        }

        initTooltips(tableElement);
        return table;
    }

    return { init: init, initServer: initServer, initTooltips: initTooltips };
})();
