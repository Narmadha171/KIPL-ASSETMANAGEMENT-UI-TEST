/* =========================================================
   KIPL Asset Management — client behaviours.
   Mirrors the prototype: modal dialogs, toasts, choice rows,
   photo capture, table sorting and filter menus.
========================================================= */
(function () {
    'use strict';

    /* ---------------------------------------------------- modals */
    // Remembers what had focus before a dialog opened, so closing it (via
    // Escape, backdrop click, or Cancel) returns keyboard focus exactly where
    // the user left off instead of dropping it back to <body>.
    var lastFocused = null;

    function openModal(name) {
        var el = document.getElementById(name) || document.getElementById('modal-' + name);
        if (!el) return;

        lastFocused = document.activeElement;

        el.classList.add('open');
        el.setAttribute('role', 'dialog');
        el.setAttribute('aria-modal', 'true');
        document.body.style.overflow = 'hidden';

        var first = el.querySelector('input:not([type=hidden]):not([disabled]), select, textarea, button:not([data-close-modal])');
        if (first) setTimeout(function () { first.focus(); }, 60);
    }

    function closeModal() {
        document.querySelectorAll('.modal-overlay.open').forEach(function (el) {
            el.classList.remove('open');
        });
        document.body.style.overflow = '';

        if (lastFocused && typeof lastFocused.focus === 'function') {
            lastFocused.focus();
        }
        lastFocused = null;
    }

    window.openModal = openModal;
    window.closeModal = closeModal;

    document.addEventListener('click', function (event) {
        var opener = event.target.closest('[data-modal]');
        if (opener) {
            event.preventDefault();
            var name = opener.getAttribute('data-modal');
            hydrateModal(name, opener);
            openModal(name);
            return;
        }

        if (event.target.closest('[data-close-modal]')) {
            event.preventDefault();
            closeModal();
            return;
        }

        // Click on the backdrop, not the dialog itself.
        if (event.target.classList && event.target.classList.contains('modal-overlay')) {
            closeModal();
        }
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape') {
            closeModal();
            return;
        }

        // Keep Tab cycling inside the open dialog rather than leaking focus
        // out to the (visually hidden) page behind it.
        if (event.key === 'Tab') {
            var open = document.querySelector('.modal-overlay.open');
            if (!open) return;

            var focusable = open.querySelectorAll(
                'a[href], button:not([disabled]), input:not([disabled]):not([type=hidden]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])');
            if (focusable.length === 0) return;

            var list = Array.prototype.slice.call(focusable);
            var firstEl = list[0];
            var lastEl = list[list.length - 1];

            if (event.shiftKey && document.activeElement === firstEl) {
                event.preventDefault();
                lastEl.focus();
            } else if (!event.shiftKey && document.activeElement === lastEl) {
                event.preventDefault();
                firstEl.focus();
            }
        }
    });

    /**
     * Copies data-* values from the button that opened the modal into any
     * matching [data-field] element or form input inside it. This is what lets
     * one modal serve every row of a table.
     */
    function hydrateModal(name, trigger) {
        var modal = document.getElementById(name) || document.getElementById('modal-' + name);
        if (!modal) return;

        Object.keys(trigger.dataset).forEach(function (key) {
            if (key === 'modal') return;
            var value = trigger.dataset[key];

            modal.querySelectorAll('[data-field="' + key + '"]').forEach(function (el) {
                el.textContent = value;
            });
            modal.querySelectorAll('[data-input="' + key + '"]').forEach(function (el) {
                el.value = value;
            });
            modal.querySelectorAll('[data-href="' + key + '"]').forEach(function (el) {
                el.setAttribute('href', el.getAttribute('data-href-base') + value);
            });
            modal.querySelectorAll('[data-src="' + key + '"]').forEach(function (el) {
                el.setAttribute('src', el.getAttribute('data-src-base') + encodeURIComponent(value));
            });
        });

        // Reset any photo capture controls each time the dialog opens.
        modal.querySelectorAll('.dropzone').forEach(function (zone) {
            zone.classList.remove('done');
            var label = zone.getAttribute('data-label');
            if (label) zone.textContent = label;
        });
    }

    /* ---------------------------------------------------- toasts */
    function toast(message, type) {
        var stack = document.getElementById('toastStack');
        if (!stack) return;

        var el = document.createElement('div');
        el.className = 'toast' + (type ? ' ' + type : '');
        var mark = type === 'success' ? '✓' : type === 'danger' ? '!' : '•';
        el.innerHTML = '<span>' + mark + '</span><span style="flex:1;"></span><button type="button" class="toast-close" style="background:none;border:none;color:inherit;cursor:pointer;opacity:.7;padding:0 0 0 8px;font-size:14px;" aria-label="Close">✕</button>';
        el.children[1].textContent = message;
        
        var timer;
        function dismiss() {
            if (timer) clearTimeout(timer);
            el.classList.add('leaving');
            setTimeout(function () { el.remove(); }, 250);
        }

        el.addEventListener('click', dismiss);
        stack.appendChild(el);

        timer = setTimeout(dismiss, 4000);
    }

    window.toast = toast;

    // Server-side messages arrive as data attributes on the stack element.
    document.addEventListener('DOMContentLoaded', function () {
        var stack = document.getElementById('toastStack');
        if (!stack) return;
        if (stack.dataset.success) toast(stack.dataset.success, 'success');
        if (stack.dataset.error) toast(stack.dataset.error, 'danger');
        if (stack.dataset.info) toast(stack.dataset.info);
        if (stack.dataset.openModal) openModal(stack.dataset.openModal);
    });

    /* --------------------------------------- mobile nav drawer */
    // Below the 860px breakpoint the sidebar becomes an off-canvas drawer,
    // toggled by the hamburger button in the topbar and closed via the
    // backdrop, Escape, or picking a nav link.
    (function () {
        var rail = document.getElementById('railNav');
        var toggle = document.querySelector('[data-rail-toggle]');
        var backdrop = document.querySelector('[data-rail-backdrop]');
        if (!rail || !toggle || !backdrop) return;

        function closeRail() {
            rail.classList.remove('open');
            backdrop.classList.remove('open');
            toggle.setAttribute('aria-expanded', 'false');
            document.body.style.overflow = '';
        }

        function openRail() {
            rail.classList.add('open');
            backdrop.classList.add('open');
            toggle.setAttribute('aria-expanded', 'true');
            document.body.style.overflow = 'hidden';
        }

        toggle.addEventListener('click', function () {
            if (rail.classList.contains('open')) closeRail(); else openRail();
        });

        backdrop.addEventListener('click', closeRail);

        rail.querySelectorAll('.nav-item').forEach(function (link) {
            link.addEventListener('click', closeRail);
        });

        document.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') closeRail();
        });
    })();

    /* ------------------------------------------- choice rows */
    // Segmented Office / Courier style pickers that drive a hidden input
    // and show or hide the fields relevant to the chosen option.
    document.addEventListener('click', function (event) {
        var choice = event.target.closest('.choice');
        if (!choice) return;

        var group = choice.closest('[data-choice-group]');
        if (!group) return;

        group.querySelectorAll('.choice').forEach(function (c) { c.classList.remove('sel'); });
        choice.classList.add('sel');

        var value = choice.getAttribute('data-value');
        var input = group.querySelector('input[type=hidden]');
        if (input) input.value = value;

        var scope = group.closest('[data-choice-scope]') || group.parentElement;
        if (!scope) return;

        scope.querySelectorAll('[data-when]').forEach(function (field) {
            var wanted = field.getAttribute('data-when').split('|');
            field.style.display = wanted.indexOf(value) >= 0 ? '' : 'none';
        });

        scope.dispatchEvent(new CustomEvent('choice:changed', { bubbles: true, detail: { value: value } }));
    });

    /* ----------------------------------------- photo capture */
    // A dropzone paired with a hidden file input, matching the prototype's
    // "capture photo" affordance but backed by a real upload.
    document.addEventListener('click', function (event) {
        var zone = event.target.closest('.dropzone[data-for]');
        if (!zone) return;
        var input = document.getElementById(zone.getAttribute('data-for'));
        if (input) input.click();
    });

    document.addEventListener('change', function (event) {
        var input = event.target;
        if (!input.matches || !input.matches('input[type=file][data-zone]')) return;

        var zone = document.getElementById(input.getAttribute('data-zone'));
        if (!zone) return;

        if (input.files && input.files.length) {
            zone.classList.add('done');
            zone.textContent = '✓ ' + input.files[0].name;
        } else {
            zone.classList.remove('done');
            zone.textContent = zone.getAttribute('data-label') || 'Capture photo';
        }
    });

    /* --------------------------------------- gated submit buttons */
    // Some flows (inspection, verification) only enable their primary button
    // once every required control has a value.
    function evaluateGates() {
        document.querySelectorAll('[data-gate]').forEach(function (form) {
            var button = form.querySelector('[data-gate-target]');
            if (!button) return;

            var ready = Array.prototype.every.call(
                form.querySelectorAll('[data-gate-required]'),
                function (el) {
                    if (el.type === 'file') return el.files && el.files.length > 0;
                    if (el.type === 'checkbox') return el.checked;
                    return el.value !== null && el.value !== '';
                });

            button.disabled = !ready;
        });
    }

    document.addEventListener('change', evaluateGates);
    document.addEventListener('input', evaluateGates);
    document.addEventListener('choice:changed', evaluateGates);
    document.addEventListener('DOMContentLoaded', evaluateGates);

    /* ------------------------------------------ misc helpers */
    document.addEventListener('submit', function (event) {
        var message = event.target.getAttribute('data-confirm');
        if (message && !window.confirm(message)) {
            event.preventDefault();
            return;
        }

        var form = event.target;
        if (event.defaultPrevented || (form.checkValidity && !form.checkValidity())) {
            return;
        }

        // Guard against a slow connection tempting someone into a second
        // click — defer disabling slightly so the browser serializes the clicked
        // button's name and value into the form payload before disabling it.
        var submitButton = form.querySelector('button[type=submit]');
        if (submitButton && !submitButton.disabled) {
            var btn = submitButton;
            setTimeout(function () {
                btn.dataset.originalLabel = btn.textContent;
                btn.disabled = true;
                btn.textContent = 'Saving…';
            }, 0);
        }
    });

    document.addEventListener('change', function (event) {
        if (event.target.matches && event.target.matches('[data-autosubmit]')) {
            var form = event.target.closest('form');
            if (form) {
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit();
                } else {
                    form.submit();
                }
            }
        }
    });

    // Select-all checkbox at the head of a table.
    document.addEventListener('change', function (event) {
        var master = event.target;
        if (!master.matches || !master.matches('[data-select-all]')) return;

        var scope = document.getElementById(master.getAttribute('data-select-all'));
        if (!scope) return;
        scope.querySelectorAll('input[type=checkbox]').forEach(function (box) {
            box.checked = master.checked;
        });
    });

    // Login page demo account shortcuts.
    document.querySelectorAll('[data-demo-email]').forEach(function (button) {
        button.addEventListener('click', function () {
            var email = document.getElementById('Input_Email');
            var password = document.getElementById('Input_Password');
            if (email) email.value = button.getAttribute('data-demo-email');
            if (password) password.value = button.getAttribute('data-demo-password');
        });
    });

    /* --------------------------------------- search hotkeys */
    document.addEventListener('keydown', function (event) {
        var isEditing = ['INPUT', 'TEXTAREA', 'SELECT'].indexOf(document.activeElement.tagName) >= 0;
        if ((event.key === '/' && !isEditing) || ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k')) {
            var searchInput = document.querySelector('.search input[name="Search"]');
            if (searchInput) {
                event.preventDefault();
                searchInput.focus();
                searchInput.select();
            }
        }
    });

    /* --------------------------------------- notifications popup */
    document.addEventListener('click', function (event) {
        if (event.target.closest('#btnMarkAllRead')) {
            event.preventDefault();
            var container = document.getElementById('notifContainer');
            var pill = document.querySelector('.notif-count-pill');
            var clearBtn = document.getElementById('btnMarkAllRead');
            var dot = document.getElementById('topbarNotifDot');

            if (container) {
                container.innerHTML = '<div class="notif-empty" id="notifEmptyState"><div style="font-size: 28px; margin-bottom: 6px;">🎉</div><div style="font-weight: 600; font-size: 13.5px;">All caught up!</div><div class="cell-muted" style="font-size: 11.5px; margin-top: 3px;">You have no new notifications right now.</div></div>';
            }
            if (pill) pill.remove();
            if (clearBtn) clearBtn.remove();
            if (dot) dot.remove();
            
            toast('Notifications marked as read', 'success');
        }
    });

    window.printChallan = function () { window.print(); };
})();
