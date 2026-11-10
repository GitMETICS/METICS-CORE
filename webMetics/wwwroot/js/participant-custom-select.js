(function () {
    'use strict';

    function initializeParticipantEditSelects() {
        var root = document.querySelector('.edit-participant-page');
        if (!root) return;

        var instances = [];

        function closeOthers(current) {
            instances.forEach(function (instance) {
                if (instance !== current) instance.close(false);
            });
        }

        Array.prototype.forEach.call(root.querySelectorAll('select:not([hidden])'), function (select, index) {
            if (select.dataset.participantSelectReady === 'true') return;

            var baseId = select.id || select.name || ('editParticipantSelect' + index);
            var field = select.closest('.form-group');
            var label = field ? field.querySelector('label') : null;
            var validation = field ? field.querySelector('[data-valmsg-for="' + select.name + '"]') : null;
            var wrapper = document.createElement('div');
            wrapper.className = 'participant-custom-select';
            select.parentNode.insertBefore(wrapper, select);
            wrapper.appendChild(select);
            select.classList.add('participant-native-select');
            select.setAttribute('aria-hidden', 'true');
            select.setAttribute('tabindex', '-1');
            select.dataset.participantSelectReady = 'true';

            var trigger = document.createElement('button');
            trigger.type = 'button';
            trigger.id = baseId + 'ParticipantTrigger';
            trigger.className = 'participant-select-trigger';
            trigger.setAttribute('role', 'combobox');
            trigger.setAttribute('aria-haspopup', 'listbox');
            trigger.setAttribute('aria-expanded', 'false');
            trigger.setAttribute('aria-controls', baseId + 'ParticipantListbox');
            if (label) {
                if (!label.id) label.id = baseId + 'ParticipantLabel';
                trigger.setAttribute('aria-labelledby', label.id);
                label.addEventListener('click', function (event) {
                    event.preventDefault();
                    if (!trigger.disabled) {
                        trigger.focus();
                        opened ? close(false) : open();
                    }
                });
            } else {
                trigger.setAttribute('aria-label', select.name || 'Seleccionar una opción');
            }
            if (select.getAttribute('aria-describedby')) trigger.setAttribute('aria-describedby', select.getAttribute('aria-describedby'));

            var value = document.createElement('span');
            value.className = 'participant-select-value';
            var arrow = document.createElement('span');
            arrow.className = 'participant-select-arrow';
            arrow.setAttribute('aria-hidden', 'true');
            trigger.append(value, arrow);

            var panel = document.createElement('div');
            panel.id = baseId + 'ParticipantListbox';
            panel.className = 'participant-select-panel';
            panel.setAttribute('role', 'listbox');
            panel.hidden = true;
            wrapper.append(trigger, panel);

            var rows = [];
            var activeIndex = -1;
            var opened = false;
            var typeAhead = '';
            var typeAheadTimeout;

            function renderOptions() {
                panel.replaceChildren();
                rows = [];
                Array.prototype.forEach.call(select.options, function (option, optionIndex) {
                    var row = document.createElement('div');
                    row.id = panel.id + 'Option' + optionIndex;
                    row.className = 'participant-select-option';
                    row.setAttribute('role', 'option');
                    row.setAttribute('aria-selected', option.selected ? 'true' : 'false');
                    row.setAttribute('aria-disabled', option.disabled ? 'true' : 'false');
                    row.textContent = option.text;
                    if (option.selected) row.classList.add('is-selected');
                    row.addEventListener('mouseenter', function () { if (!option.disabled) setActive(optionIndex); });
                    row.addEventListener('mousedown', function (event) { event.preventDefault(); });
                    row.addEventListener('click', function () { if (!option.disabled) choose(optionIndex); });
                    panel.appendChild(row);
                    rows.push(row);
                });
            }

            function sync() {
                renderOptions();
                var selected = select.options[select.selectedIndex];
                value.textContent = selected ? selected.text : '';
                trigger.disabled = select.disabled;
                trigger.setAttribute('aria-disabled', select.disabled ? 'true' : 'false');
                if (validation && validation.classList.contains('field-validation-error')) {
                    trigger.classList.add('input-validation-error');
                    trigger.setAttribute('aria-invalid', 'true');
                    wrapper.classList.add('has-validation-error');
                }
            }

            function setActive(optionIndex) {
                if (optionIndex < 0 || optionIndex >= rows.length || select.options[optionIndex].disabled) return;
                activeIndex = optionIndex;
                rows.forEach(function (row, i) { row.classList.toggle('is-active', i === activeIndex); });
                trigger.setAttribute('aria-activedescendant', rows[activeIndex].id);
                rows[activeIndex].scrollIntoView({ block: 'nearest' });
            }

            function firstEnabled(start, step) {
                for (var i = start; i >= 0 && i < select.options.length; i += step) {
                    if (!select.options[i].disabled) return i;
                }
                return -1;
            }

            function open() {
                if (trigger.disabled || opened) return;
                closeOthers(instance);
                sync();
                panel.hidden = false;
                wrapper.classList.add('is-open');
                var card = wrapper.closest('.create-participant-card');
                if (card) card.classList.add('participant-card-select-open');
                trigger.setAttribute('aria-expanded', 'true');
                opened = true;
                var rect = trigger.getBoundingClientRect();
                var panelHeight = Math.min(panel.scrollHeight, window.innerHeight - 24);
                wrapper.classList.toggle('is-open-above', window.innerHeight - rect.bottom < panelHeight + 14 && rect.top > panelHeight + 14);
                var start = select.selectedIndex >= 0 && !select.options[select.selectedIndex].disabled
                    ? select.selectedIndex : firstEnabled(0, 1);
                if (start >= 0) setActive(start);
            }

            function close(returnFocus) {
                if (!opened) return;
                panel.hidden = true;
                wrapper.classList.remove('is-open', 'is-open-above');
                var card = wrapper.closest('.create-participant-card');
                if (card) card.classList.remove('participant-card-select-open');
                trigger.setAttribute('aria-expanded', 'false');
                trigger.removeAttribute('aria-activedescendant');
                opened = false;
                activeIndex = -1;
                if (returnFocus) trigger.focus();
            }

            function choose(optionIndex) {
                if (optionIndex < 0 || optionIndex >= select.options.length || select.options[optionIndex].disabled) return;
                select.selectedIndex = optionIndex;
                select.dispatchEvent(new Event('input', { bubbles: true }));
                select.dispatchEvent(new Event('change', { bubbles: true }));
                sync();
                close(true);
            }

            function search(character) {
                var normalized = character.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase();
                typeAhead += normalized;
                window.clearTimeout(typeAheadTimeout);
                typeAheadTimeout = window.setTimeout(function () { typeAhead = ''; }, 800);
                for (var offset = 1; offset <= select.options.length; offset++) {
                    var i = (select.selectedIndex + offset + select.options.length) % select.options.length;
                    var option = select.options[i];
                    var text = option.text.trim().normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLocaleLowerCase();
                    if (!option.disabled && option.value !== '' && text.startsWith(typeAhead)) {
                        choose(i);
                        return;
                    }
                }
            }

            var instance = { close: close };
            instances.push(instance);
            trigger.addEventListener('click', function () { opened ? close(false) : open(); });
            trigger.addEventListener('keydown', function (event) {
                if (event.key === 'Escape') { if (opened) { event.preventDefault(); close(true); } return; }
                if (event.key === 'Tab') { close(false); return; }
                if (event.key.length === 1 && event.key.trim() && !event.ctrlKey && !event.altKey && !event.metaKey) {
                    event.preventDefault(); search(event.key); return;
                }
                if (['ArrowDown', 'ArrowUp', 'Home', 'End', 'Enter', ' '].indexOf(event.key) === -1) return;
                event.preventDefault();
                if (!opened) open();
                if (event.key === 'ArrowDown') setActive(firstEnabled(activeIndex + 1, 1));
                else if (event.key === 'ArrowUp') setActive(firstEnabled(activeIndex < 0 ? select.options.length - 1 : activeIndex - 1, -1));
                else if (event.key === 'Home') setActive(firstEnabled(0, 1));
                else if (event.key === 'End') setActive(firstEnabled(select.options.length - 1, -1));
                else if (activeIndex >= 0) choose(activeIndex);
            });
            select.addEventListener('change', sync);
            new MutationObserver(sync).observe(select, { childList: true, subtree: true, attributes: true, attributeFilter: ['disabled'] });
            sync();
        });

        document.addEventListener('pointerdown', function (event) {
            instances.forEach(function (instance) {
                var current = root.querySelector('.participant-custom-select.is-open');
                if (current && !current.contains(event.target)) instance.close(false);
            });
        });
    }

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', initializeParticipantEditSelects, { once: true });
    else initializeParticipantEditSelects();
}());
