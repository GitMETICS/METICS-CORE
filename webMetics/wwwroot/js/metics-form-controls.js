/*
 * Componentes de formulario de METICS reutilizables: calendario de fecha y lista desplegable.
 * Es el mismo comportamiento y el mismo HTML que usa la pantalla "Crear módulo" (clases create-group-*),
 * empaquetado para aplicarse dentro de un contenedor concreto:
 *
 *   MeticsFormControls.initDatePickers(contenedor);
 *   MeticsFormControls.initSelects(contenedor);
 *
 * Los controles nativos (input type="date" y select) se conservan ocultos y siguen siendo los que
 * se envían con el formulario, así que nombres, valores y validación no cambian.
 */
(function (window, document) {
    'use strict';

    var FIELD_SELECTOR = '.form-group, .create-category-field';
    var CARD_SELECTOR = '.create-group-card, .admin-panel-card';
    var CARD_OPEN_CLASS = 'create-group-card--select-open';

    function pad(number) { return String(number).padStart(2, '0'); }
    function dateKey(date) {
        return date.getFullYear() + '-' + pad(date.getMonth() + 1) + '-' + pad(date.getDate());
    }
    function parseDate(value) {
        if (!value) return null;
        var parts = value.split('-').map(Number);
        if (parts.length !== 3) return null;
        var date = new Date(parts[0], parts[1] - 1, parts[2]);
        return date.getFullYear() === parts[0] && date.getMonth() === parts[1] - 1 && date.getDate() === parts[2] ? date : null;
    }
    function sameDay(left, right) {
        return !!left && !!right && dateKey(left) === dateKey(right);
    }
    function formatDisplay(date) {
        return new Intl.DateTimeFormat('es-CR', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(date);
    }
    function findValidationMessage(control) {
        var fieldContainer = control.closest(FIELD_SELECTOR);
        return fieldContainer ? fieldContainer.querySelector('[data-valmsg-for="' + control.name + '"]') : null;
    }
    function findLabels(control) {
        return Array.prototype.filter.call(document.querySelectorAll('label'), function (label) {
            return label.htmlFor && (label.htmlFor === control.id || label.htmlFor === control.name);
        });
    }

    function initDatePickers(root) {
        if (!root) return [];
        var inputs = Array.prototype.slice.call(root.querySelectorAll('input[type="date"]'));
        var instances = [];

        inputs.forEach(function (input, inputIndex) {
            if (input.dataset.customDateReady === 'true') return;

            var baseId = input.id || input.name || ('meticsDate' + inputIndex);
            var manualError = '';
            var wrapper = document.createElement('div');
            wrapper.className = 'create-group-custom-date';
            input.parentNode.insertBefore(wrapper, input);
            wrapper.appendChild(input);
            input.classList.add('create-group-native-date');
            input.setAttribute('aria-hidden', 'true');
            input.setAttribute('tabindex', '-1');
            input.dataset.customDateReady = 'true';

            var manualInput = document.createElement('input');
            manualInput.type = 'text';
            manualInput.id = baseId + 'DateText';
            manualInput.className = 'create-group-date-text';
            manualInput.placeholder = 'dd/mm/aaaa';
            manualInput.autocomplete = 'off';
            manualInput.inputMode = 'numeric';
            manualInput.setAttribute('aria-haspopup', 'dialog');
            manualInput.setAttribute('aria-expanded', 'false');
            manualInput.setAttribute('aria-controls', baseId + 'Calendar');
            if (input.required) manualInput.setAttribute('aria-required', 'true');

            var trigger = document.createElement('button');
            trigger.type = 'button';
            trigger.id = baseId + 'CalendarTrigger';
            trigger.className = 'create-group-date-trigger';
            trigger.setAttribute('aria-label', 'Abrir calendario de fecha');
            trigger.setAttribute('aria-expanded', 'false');
            trigger.setAttribute('aria-controls', baseId + 'Calendar');

            var icon = document.createElement('i');
            icon.className = 'bi bi-calendar3';
            icon.setAttribute('aria-hidden', 'true');
            trigger.appendChild(icon);

            var panel = document.createElement('div');
            panel.id = baseId + 'Calendar';
            panel.className = 'create-group-date-panel';
            panel.setAttribute('role', 'dialog');
            panel.setAttribute('aria-label', 'Seleccionar fecha');
            panel.hidden = true;

            var header = document.createElement('div');
            header.className = 'create-group-date-header';
            var previous = document.createElement('button');
            previous.type = 'button';
            previous.className = 'create-group-date-nav';
            previous.setAttribute('aria-label', 'Mes anterior');
            previous.innerHTML = '<i class="bi bi-chevron-left" aria-hidden="true"></i>';
            var monthTitle = document.createElement('div');
            monthTitle.className = 'create-group-date-month';
            monthTitle.setAttribute('aria-live', 'polite');
            var next = document.createElement('button');
            next.type = 'button';
            next.className = 'create-group-date-nav';
            next.setAttribute('aria-label', 'Mes siguiente');
            next.innerHTML = '<i class="bi bi-chevron-right" aria-hidden="true"></i>';
            header.append(previous, monthTitle, next);

            var calendar = document.createElement('div');
            calendar.className = 'create-group-date-calendar';
            calendar.setAttribute('role', 'group');
            var footer = document.createElement('div');
            footer.className = 'create-group-date-footer';
            var todayButton = document.createElement('button');
            todayButton.type = 'button';
            todayButton.className = 'create-group-date-today';
            todayButton.textContent = 'Hoy';
            footer.appendChild(todayButton);
            panel.append(header, calendar, footer);
            wrapper.append(manualInput, trigger, panel);

            var validationMessage = findValidationMessage(input);
            if (validationMessage && !validationMessage.id) validationMessage.id = baseId + 'CalendarValidation';
            var describedBy = [input.getAttribute('aria-describedby'), validationMessage ? validationMessage.id : '']
                .filter(Boolean).join(' ');
            if (describedBy) {
                manualInput.setAttribute('aria-describedby', describedBy);
                trigger.setAttribute('aria-describedby', describedBy);
            }
            var labels = findLabels(input);
            if (labels.length) {
                var labelIds = labels.map(function (label, labelIndex) {
                    if (!label.id) label.id = baseId + 'CalendarLabel' + labelIndex;
                    return label.id;
                }).join(' ');
                manualInput.setAttribute('aria-labelledby', labelIds);
                trigger.setAttribute('aria-label', 'Abrir calendario para ' + labels.map(function (label) {
                    return label.textContent.trim();
                }).join(' '));
                labels.forEach(function (label) {
                    label.addEventListener('click', function (event) {
                        event.preventDefault();
                        manualInput.focus();
                    });
                });
            }

            var isOpen = false;
            var viewMonth = new Date();
            viewMonth.setDate(1);
            var focusedDate = null;
            var dayButtons = [];

            function parseTypedDate(value) {
                var text = value.trim();
                var isoParts = text.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/);
                var localParts = text.match(/^(\d{1,2})[/.\-](\d{1,2})[/.\-](\d{4})$/);
                var year;
                var month;
                var day;
                if (isoParts) {
                    year = Number(isoParts[1]);
                    month = Number(isoParts[2]);
                    day = Number(isoParts[3]);
                } else if (localParts) {
                    day = Number(localParts[1]);
                    month = Number(localParts[2]);
                    year = Number(localParts[3]);
                } else {
                    return null;
                }
                var date = new Date(year, month - 1, day);
                return date.getFullYear() === year && date.getMonth() === month - 1 && date.getDate() === day ? date : null;
            }
            function isAllowed(date) {
                var key = dateKey(date);
                return (!input.min || key >= input.min) && (!input.max || key <= input.max);
            }
            function setValidationText(message) {
                if (validationMessage) validationMessage.textContent = message;
            }
            function renderValue() {
                var selected = parseDate(input.value);
                if (!manualError) manualInput.value = selected ? formatDisplay(selected) : '';
                if (selected) {
                    viewMonth = new Date(selected.getFullYear(), selected.getMonth(), 1);
                }
                renderCalendar();
                var hasError = validationMessage && validationMessage.textContent.trim().length > 0;
                if (hasError) {
                    trigger.setAttribute('aria-invalid', 'true');
                    manualInput.setAttribute('aria-invalid', 'true');
                    wrapper.classList.add('has-validation-error');
                } else {
                    trigger.removeAttribute('aria-invalid');
                    manualInput.removeAttribute('aria-invalid');
                    wrapper.classList.remove('has-validation-error');
                }
            }
            function renderCalendar() {
                monthTitle.textContent = new Intl.DateTimeFormat('es-CR', { month: 'long', year: 'numeric' }).format(viewMonth);
                calendar.replaceChildren();
                dayButtons = [];
                ['L', 'M', 'X', 'J', 'V', 'S', 'D'].forEach(function (dayName) {
                    var weekday = document.createElement('span');
                    weekday.className = 'create-group-date-weekday';
                    weekday.setAttribute('role', 'columnheader');
                    weekday.textContent = dayName;
                    calendar.appendChild(weekday);
                });

                var firstDay = new Date(viewMonth.getFullYear(), viewMonth.getMonth(), 1);
                var offset = (firstDay.getDay() + 6) % 7;
                var daysInMonth = new Date(viewMonth.getFullYear(), viewMonth.getMonth() + 1, 0).getDate();
                var weeks = Math.ceil((offset + daysInMonth) / 7);
                var selectedDate = parseDate(input.value);
                var today = new Date();
                today.setHours(0, 0, 0, 0);
                todayButton.disabled = !isAllowed(today);

                for (var cell = 0; cell < weeks * 7; cell++) {
                    var date = new Date(viewMonth.getFullYear(), viewMonth.getMonth(), cell - offset + 1);
                    var dayButton = document.createElement('button');
                    dayButton.type = 'button';
                    dayButton.className = 'create-group-date-day';
                    dayButton.textContent = String(date.getDate());
                    dayButton.setAttribute('aria-label', new Intl.DateTimeFormat('es-CR', { dateStyle: 'full' }).format(date));
                    dayButton.dataset.date = dateKey(date);
                    dayButton.disabled = !isAllowed(date);
                    dayButton.tabIndex = -1;
                    if (date.getMonth() !== viewMonth.getMonth()) dayButton.classList.add('is-outside-month');
                    if (sameDay(date, selectedDate)) {
                        dayButton.classList.add('is-selected');
                        dayButton.setAttribute('aria-selected', 'true');
                    } else {
                        dayButton.setAttribute('aria-selected', 'false');
                    }
                    if (sameDay(date, today)) dayButton.classList.add('is-today');
                    dayButton.addEventListener('click', function (event) {
                        chooseDate(parseDate(event.currentTarget.dataset.date));
                    });
                    calendar.appendChild(dayButton);
                    dayButtons.push(dayButton);
                }
            }
            function setFocusedDate(date, focusButton) {
                focusedDate = date;
                if (date.getMonth() !== viewMonth.getMonth() || date.getFullYear() !== viewMonth.getFullYear()) {
                    viewMonth = new Date(date.getFullYear(), date.getMonth(), 1);
                    renderCalendar();
                }
                dayButtons.forEach(function (button) {
                    var isFocused = button.dataset.date === dateKey(date);
                    button.tabIndex = isFocused && !button.disabled ? 0 : -1;
                });
                var target = dayButtons.find(function (button) { return button.dataset.date === dateKey(date); });
                if (focusButton && target && !target.disabled) target.focus();
            }
            function open() {
                if (trigger.disabled || isOpen) return;
                instances.forEach(function (instance) { if (instance.wrapper !== wrapper) instance.close(false); });
                var now = new Date();
                focusedDate = parseDate(input.value) || new Date(now.getFullYear(), now.getMonth(), now.getDate());
                var minimumDate = parseDate(input.min);
                var maximumDate = parseDate(input.max);
                if (minimumDate && focusedDate < minimumDate) focusedDate = minimumDate;
                if (maximumDate && focusedDate > maximumDate) focusedDate = maximumDate;
                viewMonth = new Date(focusedDate.getFullYear(), focusedDate.getMonth(), 1);
                renderCalendar();
                panel.hidden = false;
                wrapper.classList.add('is-open');
                var card = wrapper.closest(CARD_SELECTOR);
                if (card) card.classList.add(CARD_OPEN_CLASS);
                trigger.setAttribute('aria-expanded', 'true');
                manualInput.setAttribute('aria-expanded', 'true');
                isOpen = true;
                var rect = trigger.getBoundingClientRect();
                var roomBelow = window.innerHeight - rect.bottom;
                wrapper.classList.toggle('is-open-above', roomBelow < panel.offsetHeight + 12 && rect.top > panel.offsetHeight + 12);
                setFocusedDate(focusedDate, true);
            }
            function close(returnFocus) {
                if (!isOpen) return;
                panel.hidden = true;
                wrapper.classList.remove('is-open', 'is-open-above');
                var card = wrapper.closest(CARD_SELECTOR);
                if (card) card.classList.remove(CARD_OPEN_CLASS);
                trigger.setAttribute('aria-expanded', 'false');
                manualInput.setAttribute('aria-expanded', 'false');
                isOpen = false;
                if (returnFocus) manualInput.focus();
            }
            function notifyChange() {
                input.dispatchEvent(new Event('input', { bubbles: true }));
                input.dispatchEvent(new Event('change', { bubbles: true }));
            }
            function chooseDate(date) {
                if (!date || !isAllowed(date)) return;
                manualError = '';
                input.setCustomValidity('');
                input.value = dateKey(date);
                notifyChange();
                if (input.validity.valid) setValidationText('');
                renderValue();
                close(true);
            }
            function changeMonth(amount) {
                viewMonth = new Date(viewMonth.getFullYear(), viewMonth.getMonth() + amount, 1);
                renderCalendar();
                var preferred = focusedDate && focusedDate.getDate() || 1;
                var last = new Date(viewMonth.getFullYear(), viewMonth.getMonth() + 1, 0).getDate();
                setFocusedDate(new Date(viewMonth.getFullYear(), viewMonth.getMonth(), Math.min(preferred, last)), false);
            }
            function toggle() { if (isOpen) close(false); else open(); }

            trigger.addEventListener('click', toggle);
            manualInput.addEventListener('input', function () {
                var typedValue = manualInput.value.trim();
                if (!typedValue) {
                    manualError = '';
                    input.setCustomValidity('');
                    input.value = '';
                    notifyChange();
                    setValidationText('');
                    renderValue();
                    return;
                }

                var typedDate = parseTypedDate(typedValue);
                if (!typedDate) {
                    // El texto aún no es una fecha válida: se marca para que el formulario no se envíe con ella.
                    manualError = 'Ingrese una fecha válida con formato dd/mm/aaaa.';
                    input.value = '';
                    input.setCustomValidity(manualError);
                    return;
                }

                manualError = '';
                input.setCustomValidity('');
                input.value = dateKey(typedDate);
                notifyChange();
                if (input.validity.valid) setValidationText('');
                renderValue();
            });
            manualInput.addEventListener('keydown', function (event) {
                if (event.key === 'ArrowDown' && !isOpen) {
                    event.preventDefault();
                    open();
                } else if (event.key === 'Escape' && isOpen) {
                    event.preventDefault();
                    close(true);
                }
            });
            previous.addEventListener('click', function () { changeMonth(-1); });
            next.addEventListener('click', function () { changeMonth(1); });
            todayButton.addEventListener('click', function () {
                var now = new Date();
                chooseDate(new Date(now.getFullYear(), now.getMonth(), now.getDate()));
            });
            trigger.addEventListener('keydown', function (event) {
                if (event.key === 'Escape' && isOpen) { event.preventDefault(); close(true); }
                else if (event.key === 'ArrowDown' || event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    if (!isOpen) open();
                }
            });
            panel.addEventListener('keydown', function (event) {
                if (event.key === 'Escape') { event.preventDefault(); close(true); return; }
                var delta = event.key === 'ArrowLeft' ? -1 : event.key === 'ArrowRight' ? 1 : event.key === 'ArrowUp' ? -7 : event.key === 'ArrowDown' ? 7 : 0;
                if (delta) {
                    event.preventDefault();
                    if (input.min && input.max && input.min > input.max) return;
                    var nextDate = new Date(focusedDate.getFullYear(), focusedDate.getMonth(), focusedDate.getDate() + delta);
                    var attempts = 0;
                    while (!isAllowed(nextDate) && attempts < 3700) {
                        var nextKey = dateKey(nextDate);
                        if (input.min && nextKey < input.min) nextDate = parseDate(input.min);
                        else if (input.max && nextKey > input.max) nextDate = parseDate(input.max);
                        else nextDate.setDate(nextDate.getDate() + Math.sign(delta));
                        if (!nextDate) break;
                        attempts++;
                    }
                    if (attempts < 3700 && isAllowed(nextDate)) setFocusedDate(nextDate, true);
                } else if (event.key === 'Enter' && event.target.classList.contains('create-group-date-day')) {
                    event.preventDefault();
                    chooseDate(parseDate(event.target.dataset.date));
                }
            });
            input.addEventListener('change', renderValue);
            input.addEventListener('invalid', function (event) {
                event.preventDefault();
                var message;
                if (input.validity.valueMissing) message = 'Selecciona la fecha indicada.';
                else if (input.validity.rangeUnderflow && parseDate(input.min)) message = 'La fecha no puede ser anterior al ' + formatDisplay(parseDate(input.min)) + '.';
                else if (input.validity.rangeOverflow && parseDate(input.max)) message = 'La fecha no puede ser posterior al ' + formatDisplay(parseDate(input.max)) + '.';
                else message = manualError || input.validationMessage || 'Selecciona una fecha válida.';
                setValidationText(message);
                trigger.setAttribute('aria-invalid', 'true');
                manualInput.setAttribute('aria-invalid', 'true');
                wrapper.classList.add('has-validation-error');
                window.setTimeout(function () { manualInput.focus(); }, 0);
            });
            var instance = { close: close, wrapper: wrapper, sync: renderValue, input: input };
            instances.push(instance);
            renderValue();
        });

        document.addEventListener('pointerdown', function (event) {
            instances.forEach(function (instance) {
                if (!instance.wrapper.contains(event.target)) instance.close(false);
            });
        });
        document.addEventListener('focusin', function (event) {
            instances.forEach(function (instance) {
                if (!instance.wrapper.contains(event.target)) instance.close(false);
            });
        });

        return instances;
    }

    function initSelects(root) {
        if (!root) return [];
        var selects = Array.prototype.slice.call(root.querySelectorAll('select:not([hidden])'));
        var instances = [];

        function closeAll(except) {
            instances.forEach(function (instance) {
                if (instance !== except) instance.close(false);
            });
        }

        selects.forEach(function (select, selectIndex) {
            if (select.dataset.customSelectReady === 'true') return;

            var baseId = select.id || select.name || ('meticsSelect' + selectIndex);
            var wrapper = document.createElement('div');
            wrapper.className = 'create-group-custom-select';

            select.parentNode.insertBefore(wrapper, select);
            wrapper.appendChild(select);
            select.classList.add('create-group-native-select');
            select.setAttribute('aria-hidden', 'true');
            select.setAttribute('tabindex', '-1');
            select.dataset.customSelectReady = 'true';

            var trigger = document.createElement('button');
            trigger.type = 'button';
            trigger.id = baseId + 'CustomTrigger';
            trigger.className = 'create-group-select-trigger';
            trigger.setAttribute('role', 'combobox');
            trigger.setAttribute('aria-haspopup', 'listbox');
            trigger.setAttribute('aria-expanded', 'false');
            trigger.setAttribute('aria-controls', baseId + 'CustomListbox');

            var selectedText = document.createElement('span');
            selectedText.className = 'create-group-select-value';
            var arrow = document.createElement('span');
            arrow.className = 'create-group-select-arrow';
            arrow.setAttribute('aria-hidden', 'true');
            trigger.appendChild(selectedText);
            trigger.appendChild(arrow);

            var panel = document.createElement('div');
            panel.id = baseId + 'CustomListbox';
            panel.className = 'create-group-select-panel';
            panel.setAttribute('role', 'listbox');
            panel.hidden = true;

            var validationMessage = findValidationMessage(select);
            if (validationMessage) {
                if (!validationMessage.id) validationMessage.id = baseId + 'CustomValidation';
                trigger.setAttribute('aria-describedby', validationMessage.id);
            }

            wrapper.appendChild(trigger);
            wrapper.appendChild(panel);

            var labels = findLabels(select);
            if (labels.length) {
                trigger.setAttribute('aria-labelledby', labels.map(function (label, labelIndex) {
                    if (!label.id) label.id = baseId + 'CustomLabel' + labelIndex;
                    return label.id;
                }).join(' '));
                labels.forEach(function (label) {
                    label.addEventListener('click', function (event) {
                        event.preventDefault();
                        if (!trigger.disabled) {
                            trigger.focus();
                            toggle();
                        }
                    });
                });
            } else {
                trigger.setAttribute('aria-label', select.name || 'Seleccionar una opción');
            }

            var activeIndex = -1;
            var optionRows = [];
            var isOpen = false;
            var typeAheadBuffer = '';
            var typeAheadTimer = null;

            function enabledIndexFrom(startIndex, step) {
                var index = startIndex;
                while (index >= 0 && index < select.options.length) {
                    if (!select.options[index].disabled) return index;
                    index += step;
                }
                return -1;
            }

            function renderOptions() {
                panel.replaceChildren();
                optionRows = [];

                Array.prototype.forEach.call(select.options, function (nativeOption, optionIndex) {
                    var row = document.createElement('div');
                    row.id = panel.id + 'Option' + optionIndex;
                    row.className = 'create-group-select-option';
                    row.setAttribute('role', 'option');
                    row.setAttribute('aria-selected', nativeOption.selected ? 'true' : 'false');
                    row.setAttribute('aria-disabled', nativeOption.disabled ? 'true' : 'false');
                    row.dataset.optionIndex = String(optionIndex);
                    row.textContent = nativeOption.text;

                    if (nativeOption.selected) row.classList.add('is-selected');
                    if (nativeOption.disabled) row.tabIndex = -1;

                    row.addEventListener('mouseenter', function () {
                        if (!nativeOption.disabled) setActive(optionIndex, false);
                    });
                    row.addEventListener('mousedown', function (event) {
                        event.preventDefault();
                    });
                    row.addEventListener('click', function () {
                        if (!nativeOption.disabled) choose(optionIndex);
                    });

                    panel.appendChild(row);
                    optionRows.push(row);
                });
            }

            function updateSelection(clearValidation) {
                var selectedOption = select.options[select.selectedIndex];
                selectedText.textContent = selectedOption ? selectedOption.text : '';
                optionRows.forEach(function (row, index) {
                    var selected = index === select.selectedIndex;
                    row.classList.toggle('is-selected', selected);
                    row.setAttribute('aria-selected', selected ? 'true' : 'false');
                });

                if (clearValidation && select.value !== '' && validationMessage) {
                    validationMessage.textContent = '';
                }
                var hasValidationMessage = validationMessage && validationMessage.textContent.trim().length > 0;
                if (hasValidationMessage) {
                    trigger.setAttribute('aria-invalid', 'true');
                    wrapper.classList.add('has-validation-error');
                } else {
                    trigger.removeAttribute('aria-invalid');
                    wrapper.classList.remove('has-validation-error');
                }
            }

            function syncDisabled() {
                trigger.disabled = select.disabled;
                trigger.setAttribute('aria-disabled', select.disabled ? 'true' : 'false');
                if (select.disabled) close(false);
            }

            function syncFromNative() {
                renderOptions();
                updateSelection(true);
                syncDisabled();
            }

            function setActive(index, scrollIntoView) {
                if (index < 0 || index >= optionRows.length || select.options[index].disabled) return;
                activeIndex = index;
                optionRows.forEach(function (row, rowIndex) {
                    row.classList.toggle('is-active', rowIndex === activeIndex);
                });
                trigger.setAttribute('aria-activedescendant', optionRows[activeIndex].id);
                if (scrollIntoView) optionRows[activeIndex].scrollIntoView({ block: 'nearest' });
            }

            function open() {
                if (trigger.disabled || isOpen) return;
                closeAll(instance);
                renderOptions();
                panel.hidden = false;
                wrapper.classList.add('is-open');
                var card = wrapper.closest(CARD_SELECTOR);
                if (card) card.classList.add(CARD_OPEN_CLASS);
                trigger.setAttribute('aria-expanded', 'true');
                isOpen = true;

                var triggerRect = trigger.getBoundingClientRect();
                var panelHeight = Math.min(panel.scrollHeight, window.innerHeight - 24);
                var roomBelow = window.innerHeight - triggerRect.bottom;
                wrapper.classList.toggle('is-open-above', roomBelow < panelHeight + 14 && triggerRect.top > panelHeight + 14);

                var selectedIndex = select.selectedIndex;
                if (selectedIndex < 0 || select.options[selectedIndex].disabled) {
                    selectedIndex = enabledIndexFrom(0, 1);
                }
                if (selectedIndex >= 0) setActive(selectedIndex, true);
            }

            function close(returnFocus) {
                if (!isOpen) return;
                panel.hidden = true;
                wrapper.classList.remove('is-open', 'is-open-above');
                var card = wrapper.closest(CARD_SELECTOR);
                if (card) card.classList.remove(CARD_OPEN_CLASS);
                trigger.setAttribute('aria-expanded', 'false');
                trigger.removeAttribute('aria-activedescendant');
                isOpen = false;
                activeIndex = -1;
                if (returnFocus) trigger.focus();
            }

            function choose(index) {
                if (index < 0 || index >= select.options.length || select.options[index].disabled) return;
                if (select.selectedIndex !== index) {
                    select.selectedIndex = index;
                    select.dispatchEvent(new Event('input', { bubbles: true }));
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                }
                updateSelection();
                syncDisabled();
                close(true);
            }

            function toggle() {
                if (isOpen) close(false);
                else open();
            }

            function moveActive(step) {
                var start = activeIndex;
                if (start < 0) start = step > 0 ? -1 : select.options.length;
                var nextIndex = start + step;
                while (nextIndex >= 0 && nextIndex < select.options.length && select.options[nextIndex].disabled) nextIndex += step;
                if (nextIndex >= 0 && nextIndex < select.options.length) setActive(nextIndex, true);
            }

            function findTypeAheadMatch(query) {
                var startIndex = select.selectedIndex;
                for (var offset = 1; offset <= select.options.length; offset++) {
                    var optionIndex = (startIndex + offset + select.options.length) % select.options.length;
                    var option = select.options[optionIndex];
                    var optionText = option.text.trim().normalize('NFD').replace(/[̀-ͯ]/g, '').toLocaleLowerCase();
                    if (!option.disabled && option.value !== '' && optionText.startsWith(query)) {
                        return optionIndex;
                    }
                }
                return -1;
            }

            function searchByTyping(character) {
                var normalizedCharacter = character.normalize('NFD').replace(/[̀-ͯ]/g, '').toLocaleLowerCase();
                var repeatedCharacter = typeAheadBuffer && Array.from(typeAheadBuffer).every(function (letter) {
                    return letter === normalizedCharacter;
                });
                typeAheadBuffer = repeatedCharacter ? normalizedCharacter : typeAheadBuffer + normalizedCharacter;
                window.clearTimeout(typeAheadTimer);
                typeAheadTimer = window.setTimeout(function () { typeAheadBuffer = ''; }, 800);

                var match = findTypeAheadMatch(typeAheadBuffer);
                if (match < 0 && typeAheadBuffer !== normalizedCharacter) {
                    typeAheadBuffer = normalizedCharacter;
                    match = findTypeAheadMatch(typeAheadBuffer);
                }
                if (match >= 0) choose(match);
            }

            var instance = { close: close, select: select, wrapper: wrapper, syncDisabled: syncDisabled, syncFromNative: syncFromNative };
            instances.push(instance);

            trigger.addEventListener('click', toggle);
            trigger.addEventListener('keydown', function (event) {
                if (event.key === 'Escape') {
                    if (isOpen) {
                        event.preventDefault();
                        close(true);
                    }
                    return;
                }

                if (event.key === 'Tab') {
                    close(false);
                    return;
                }

                if (event.key.length === 1 && event.key.trim() && !event.ctrlKey && !event.altKey && !event.metaKey) {
                    event.preventDefault();
                    searchByTyping(event.key);
                    return;
                }

                if (['ArrowDown', 'ArrowUp', 'Home', 'End', 'Enter', ' '].indexOf(event.key) === -1) return;
                event.preventDefault();

                if (!isOpen) {
                    open();
                    if (event.key === 'ArrowDown') setActive(enabledIndexFrom(select.selectedIndex + 1, 1), true);
                    if (event.key === 'ArrowUp') setActive(enabledIndexFrom(select.selectedIndex - 1, -1), true);
                    return;
                }

                if (event.key === 'ArrowDown') moveActive(1);
                else if (event.key === 'ArrowUp') moveActive(-1);
                else if (event.key === 'Home') setActive(enabledIndexFrom(0, 1), true);
                else if (event.key === 'End') setActive(enabledIndexFrom(select.options.length - 1, -1), true);
                else if (activeIndex >= 0) choose(activeIndex);
            });

            select.addEventListener('change', function () {
                syncFromNative();
            });
            select.addEventListener('invalid', function (event) {
                event.preventDefault();
                if (validationMessage) validationMessage.textContent = select.value === ''
                    ? 'Selecciona una opción.'
                    : select.validationMessage || 'Selecciona una opción válida.';
                trigger.setAttribute('aria-invalid', 'true');
                wrapper.classList.add('has-validation-error');
                window.setTimeout(function () { trigger.focus(); }, 0);
            });

            var disabledObserver = new MutationObserver(syncDisabled);
            disabledObserver.observe(select, { attributes: true, attributeFilter: ['disabled'] });
            renderOptions();
            updateSelection();
            syncDisabled();

            // Listas que el código de la página llena o cambia (por ejemplo, selects en cadena cargados por AJAX):
            // si cambian las opciones se vuelve a dibujar la lista, y si el valor se cambia por código
            // (sin evento "change") se actualiza el texto mostrado. Solo afecta la presentación.
            var ultimoIndice = select.selectedIndex;
            var optionsObserver = new MutationObserver(function () {
                renderOptions();
                updateSelection();
                ultimoIndice = select.selectedIndex;
            });
            optionsObserver.observe(select, { childList: true, subtree: true, characterData: true });
            window.setInterval(function () {
                if (!document.body.contains(select)) return;
                if (select.selectedIndex !== ultimoIndice) {
                    ultimoIndice = select.selectedIndex;
                    updateSelection();
                }
            }, 250);
        });

        document.addEventListener('pointerdown', function (event) {
            instances.forEach(function (instance) {
                if (!instance.wrapper.contains(event.target)) instance.close(false);
            });
        });
        document.addEventListener('focusin', function (event) {
            instances.forEach(function (instance) {
                if (!instance.wrapper.contains(event.target)) instance.close(false);
            });
        });

        return instances;
    }

    window.MeticsFormControls = {
        initDatePickers: initDatePickers,
        initSelects: initSelects
    };
})(window, document);
