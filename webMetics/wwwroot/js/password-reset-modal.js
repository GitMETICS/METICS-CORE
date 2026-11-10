(function () {
    'use strict';
    if (window.meticsPasswordResetLoaded) return;
    window.meticsPasswordResetLoaded = true;

    let loading = false;

    function validatePasswordReset(form) {
        const password = form.elements.nuevaContrasena;
        const confirmation = form.elements.confirmarContrasena;
        const feedback = [
            form.querySelector('[data-valmsg-for="nuevaContrasena"]'),
            form.querySelector('[data-valmsg-for="confirmarContrasena"]')
        ];
        const messages = [
            password.value.trim() ? '' : 'Ingresa la nueva contraseña.',
            confirmation.value.trim() ? '' : 'Confirma la nueva contraseña.'
        ];

        if (!messages[0] && !messages[1] && password.value !== confirmation.value) {
            messages[1] = 'Las nuevas contraseñas no coinciden.';
        }

        let firstInvalid = null;
        [password, confirmation].forEach(function (field, index) {
            const message = messages[index];
            field.classList.toggle('input-validation-error', !!message);
            if (message) field.setAttribute('aria-invalid', 'true');
            else field.removeAttribute('aria-invalid');
            feedback[index].textContent = message;
            feedback[index].classList.toggle('field-validation-error', !!message);
            feedback[index].classList.toggle('field-validation-valid', !message);
            if (message && !firstInvalid) firstInvalid = field;
        });
        return firstInvalid;
    }

    function showToastsFromDocument(parsedDocument) {
        const toasts = Array.from(parsedDocument.querySelectorAll('.metics-toast'));
        if (!toasts.length) return;

        let region = document.querySelector('.metics-toast-region');
        if (!region) {
            region = document.createElement('div');
            region.className = 'metics-toast-region';
            region.setAttribute('aria-label', 'Notificaciones');
            document.body.appendChild(region);
        }

        toasts.forEach(function (toast) {
            region.appendChild(toast);
            if (window.bootstrap && window.bootstrap.Toast) {
                window.bootstrap.Toast.getOrCreateInstance(toast).show();
            }
        });
    }

    function showClientError(message) {
        const parsed = new DOMParser().parseFromString(
            '<div class="metics-toast metics-toast--error toast fade" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="6750">' +
            '<div class="metics-toast-inner"><span class="metics-toast-icon" aria-hidden="true"><i class="bi bi-exclamation-lg"></i></span>' +
            '<div class="metics-toast-message"><strong>No se pudo completar la operación</strong><p></p></div>' +
            '<button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Cerrar notificación"></button></div></div>',
            'text/html');
        parsed.querySelector('.metics-toast-message p').textContent = message;
        showToastsFromDocument(parsed);
    }

    document.addEventListener('click', async function (event) {
        const toggle = event.target.closest('[data-password-reset-toggle]');
        if (toggle) {
            const input = document.getElementById(toggle.dataset.passwordResetToggle);
            if (!input) return;
            const visible = input.type === 'password';
            input.type = visible ? 'text' : 'password';
            toggle.setAttribute('aria-pressed', String(visible));
            toggle.setAttribute('aria-label', visible ? 'Ocultar contraseña' : 'Mostrar contraseña');
            toggle.querySelector('i').className = visible ? 'bi bi-eye-slash' : 'bi bi-eye';
            return;
        }

        const link = event.target.closest('[data-password-reset-open]');
        if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        if (loading) return;
        loading = true;

        try {
            const response = await fetch(link.href, { credentials: 'same-origin' });
            if (!response.ok) throw new Error('load');
            const parsed = new DOMParser().parseFromString(await response.text(), 'text/html');
            const modal = parsed.getElementById('passwordResetModal');
            if (!modal) throw new Error('load');

            const previous = document.getElementById('passwordResetModal');
            if (previous) {
                window.bootstrap.Modal.getInstance(previous)?.dispose();
                previous.remove();
            }
            modal.dataset.fromList = 'true';
            document.body.appendChild(modal);
            window.bootstrap.Modal.getOrCreateInstance(modal).show();
        } catch (_) {
            window.location.assign(link.href);
        } finally {
            loading = false;
        }
    });

    document.addEventListener('hidden.bs.modal', function (event) {
        if (event.target.id !== 'passwordResetModal') return;
        if (event.target.dataset.fromList === 'true') {
            window.bootstrap.Modal.getInstance(event.target)?.dispose();
            event.target.remove();
        } else {
            window.location.assign('/Participante/VerParticipantes');
        }
    });

    document.addEventListener('input', function (event) {
        const form = event.target.closest('[data-password-reset-form]');
        if (form && form.dataset.validationAttempted === 'true') validatePasswordReset(form);
    });

    document.addEventListener('submit', async function (event) {
        const form = event.target.closest('[data-password-reset-form]');
        if (!form) return;
        event.preventDefault();

        form.dataset.validationAttempted = 'true';
        const firstInvalid = validatePasswordReset(form);
        if (firstInvalid) {
            firstInvalid.focus();
            return;
        }
        if (form.dataset.saving === 'true') return;

        form.dataset.saving = 'true';
        const buttons = Array.from(form.querySelectorAll('button'));
        const submit = form.querySelector('button[type="submit"]');
        const originalText = submit.textContent.trim();
        const data = new FormData(form);
        buttons.forEach(function (button) { button.disabled = true; });
        submit.textContent = 'Guardando...';

        try {
            const response = await fetch(form.action, {
                method: 'POST',
                body: data,
                credentials: 'same-origin'
            });
            if (!response.ok) throw new Error('save');

            const parsed = new DOMParser().parseFromString(await response.text(), 'text/html');
            const result = parsed.getElementById('passwordResetModal');
            if (!result) throw new Error('save');
            showToastsFromDocument(parsed);

            if (result.dataset.success === 'true') {
                form.elements.nuevaContrasena.value = '';
                form.elements.confirmarContrasena.value = '';
                const modal = form.closest('.modal');
                if (modal.dataset.fromList === 'true') {
                    window.bootstrap.Modal.getInstance(modal).hide();
                } else {
                    window.location.assign(response.url);
                }
            }
        } catch (_) {
            showClientError('No se pudo confirmar el guardado. Revisa la conexión e inténtalo de nuevo.');
        } finally {
            delete form.dataset.saving;
            buttons.forEach(function (button) { button.disabled = false; });
            if (submit.isConnected) submit.textContent = originalText;
        }
    });

    document.addEventListener('DOMContentLoaded', function () {
        const modal = document.getElementById('passwordResetModal');
        if (modal && modal.dataset.success !== 'true') {
            window.bootstrap.Modal.getOrCreateInstance(modal).show();
        }
    });
}());
