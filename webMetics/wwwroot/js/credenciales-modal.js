(function () {
    'use strict';
    if (window.meticsCredentialsLoaded) return;
    window.meticsCredentialsLoaded = true;
    let loading = false;
    function validateCredentials(form) {
        const fields = [form.elements.id, form.elements.nuevaContrasena, form.elements.confirmarContrasena];
        const messages = ['El identificador es obligatorio.', 'La nueva contraseña es obligatoria.', 'Confirma la nueva contraseña.'];
        let firstInvalid = null;
        fields.forEach(function (field, index) {
            let message = !field.value.trim() ? messages[index] : '';
            if (!message && field.maxLength > 0 && field.value.length > field.maxLength) message = 'No puede superar los ' + field.maxLength + ' caracteres.';
            if (!message && index === 2 && field.value !== form.elements.nuevaContrasena.value) message = 'Las contraseñas no coinciden.';
            const feedback = form.querySelector('[data-valmsg-for="' + field.name + '"]');
            field.classList.toggle('input-validation-error', !!message);
            field.setAttribute('aria-invalid', message ? 'true' : 'false');
            feedback.textContent = message;
            feedback.classList.toggle('field-validation-error', !!message);
            feedback.classList.toggle('field-validation-valid', !message);
            if (message && !firstInvalid) firstInvalid = field;
        });
        return firstInvalid;
    }
    document.addEventListener('input', function (event) {
        const form = event.target.closest('[data-credentials-form]');
        if (form && form.dataset.validationAttempted === 'true') validateCredentials(form);
    });
    document.addEventListener('click', async function (event) {
        const toggle = event.target.closest('[data-credentials-toggle]');
        if (toggle) {
            const input = document.getElementById(toggle.dataset.credentialsToggle);
            const visible = input.type === 'password';
            input.type = visible ? 'text' : 'password';
            toggle.setAttribute('aria-pressed', String(visible));
            toggle.setAttribute('aria-label', visible ? 'Ocultar contraseña' : 'Mostrar contraseña');
            toggle.querySelector('i').className = visible ? 'bi bi-eye-slash' : 'bi bi-eye';
            return;
        }
        const link = event.target.closest('[data-credentials-open]');
        if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        if (loading) return;
        loading = true;
        try {
            const response = await fetch(link.href, { credentials: 'same-origin' });
            if (!response.ok) throw new Error('load');
            const page = new DOMParser().parseFromString(await response.text(), 'text/html');
            const modal = page.getElementById('credencialesModal');
            if (!modal) throw new Error('load');
            const previous = document.getElementById('credencialesModal');
            if (previous) { bootstrap.Modal.getInstance(previous)?.dispose(); previous.remove(); }
            modal.dataset.fromList = 'true';
            document.body.appendChild(modal);
            bootstrap.Modal.getOrCreateInstance(modal).show();
        } catch (_) {
            window.location.assign(link.href);
        } finally { loading = false; }
    });
    document.addEventListener('hidden.bs.modal', function (event) {
        if (event.target.id !== 'credencialesModal') return;
        if (event.target.dataset.fromList === 'true') {
            bootstrap.Modal.getInstance(event.target)?.dispose();
            event.target.remove();
        } else {
            window.location.assign('/Participante/VerParticipantes');
        }
    });
    document.addEventListener('submit', async function (event) {
        const form = event.target.closest('[data-credentials-form]');
        if (!form) return;
        event.preventDefault();
        const error = form.querySelector('[data-credentials-error]');
        error.textContent = '';
        const password = form.elements.nuevaContrasena;
        const confirmation = form.elements.confirmarContrasena;
        form.dataset.validationAttempted = 'true';
        const firstInvalid = validateCredentials(form);
        if (firstInvalid) { firstInvalid.focus(); return; }
        if (form.dataset.saving === 'true') return;
        form.dataset.saving = 'true';
        const buttons = Array.from(form.querySelectorAll('button'));
        const data = new FormData(form);
        buttons.forEach(button => { button.disabled = true; });
        try {
            const response = await fetch(form.action, { method: 'POST', body: data, credentials: 'same-origin' });
            if (!response.ok) throw new Error('save');
            const page = new DOMParser().parseFromString(await response.text(), 'text/html');
            const result = page.getElementById('credencialesModal');
            if (!result) throw new Error('save');
            const region = page.querySelector('.metics-toast-region');
            if (region) {
                document.body.appendChild(region);
                region.querySelectorAll('.toast').forEach(toast => {
                    toast.addEventListener('hidden.bs.toast', () => { bootstrap.Toast.getInstance(toast)?.dispose(); toast.remove(); if (!region.children.length) region.remove(); });
                    bootstrap.Toast.getOrCreateInstance(toast).show();
                });
            }
            if (result.dataset.success === 'true') {
                document.dispatchEvent(new CustomEvent('credentials:saved'));
                password.value = ''; confirmation.value = '';
                bootstrap.Modal.getInstance(form.closest('.modal')).hide();
            } else {
                error.textContent = page.querySelector('.metics-toast-message p')?.textContent || 'No se pudieron actualizar las credenciales. Revisa los datos e inténtalo de nuevo.';
            }
        } catch (_) {
            error.textContent = 'No se pudo confirmar el guardado. Revisa la conexión y verifica los datos antes de volver a intentarlo.';
        } finally {
            delete form.dataset.saving;
            buttons.forEach(button => { button.disabled = false; });
        }
    });
}());
