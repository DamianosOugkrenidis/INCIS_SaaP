// INCIS SaaP — Site JavaScript (lightweight helpers)
// Author: Damianos Pythagoras Ougkrenidis
//
// PWA registration, password toggles, common utilities.
// SignalR & Formula Builder are loaded only on demand (see ErpUI in erp-ui.js).

(function () {
    'use strict';

    // ============================================
    // PWA Service Worker (best-effort)
    // ============================================
    if ('serviceWorker' in navigator && location.protocol === 'https:') {
        window.addEventListener('load', async () => {
            try {
                await navigator.serviceWorker.register('/js/service-worker.js');
            } catch (e) { /* silent */ }
        });
    }

    // ============================================
    // Password show/hide toggle
    // ============================================
    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('.toggle-password').forEach(toggle => {
            toggle.addEventListener('click', function (e) {
                e.preventDefault();
                const wrap = this.closest('.input-group') || this.parentElement;
                const input = wrap ? wrap.querySelector('input[type="password"], input[type="text"]') : null;
                const icon = this.querySelector('i');
                if (!input) return;
                if (input.type === 'password') {
                    input.type = 'text';
                    if (icon) { icon.classList.remove('fa-eye'); icon.classList.add('fa-eye-slash'); }
                } else {
                    input.type = 'password';
                    if (icon) { icon.classList.remove('fa-eye-slash'); icon.classList.add('fa-eye'); }
                }
            });
        });

        // Auto-dismiss flash alerts after 5s
        document.querySelectorAll('.alert-auto-dismiss').forEach(a => {
            setTimeout(() => {
                a.classList.remove('show');
                setTimeout(() => a.remove(), 300);
            }, 5000);
        });

        // AntiForgery: attach token to AJAX requests with header
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        if (tokenInput && window.jQuery) {
            jQuery.ajaxSetup({
                beforeSend: function (xhr) {
                    xhr.setRequestHeader('X-CSRF-TOKEN', tokenInput.value);
                }
            });
        }
    });

    // ============================================
    // Utility helpers (used across pages)
    // ============================================
    window.copyToClipboard = function (text, successMsg) {
        if (!navigator.clipboard) return;
        navigator.clipboard.writeText(text).then(() => {
            if (window.ErpUI) window.ErpUI.toast(successMsg || 'Αντιγράφηκε', 'success');
        });
    };

    window.confirmDelete = function (msg) {
        return window.confirm(msg || 'Επιβεβαιώνετε τη διαγραφή;');
    };

    window.debounce = function (fn, wait) {
        let t;
        return function () {
            const args = arguments;
            clearTimeout(t);
            t = setTimeout(() => fn.apply(this, args), wait || 200);
        };
    };

    window.formatNumber = function (n, fractionDigits) {
        if (n == null || isNaN(n)) return '';
        return Number(n).toLocaleString('el-GR', {
            minimumFractionDigits: fractionDigits || 0,
            maximumFractionDigits: fractionDigits != null ? fractionDigits : 2
        });
    };
})();
