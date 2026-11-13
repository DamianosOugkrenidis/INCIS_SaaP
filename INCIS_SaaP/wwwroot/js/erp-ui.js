/*!
 * INCIS SaaP — ERP UI Enhancements
 * Sidebar, theme toggle, toasts, DataTables defaults, search modal.
 * Author: Damianos Pythagoras Ougkrenidis
 */
(function () {
    'use strict';

    const STORAGE_KEY = 'incis-erp-ui';
    const THEME_KEY = 'incis-theme';
    const SIDEBAR_KEY = 'incis-sidebar';

    /* ========================================================== Helpers */
    const $ = (sel, root) => (root || document).querySelector(sel);
    const $$ = (sel, root) => Array.from((root || document).querySelectorAll(sel));

    function readStorage() {
        try { return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}'); }
        catch { return {}; }
    }
    function writeStorage(state) {
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify(state || {})); } catch {}
    }

    /* ========================================================== Theme */
    function applyTheme(theme) {
        const html = document.documentElement;
        html.setAttribute('data-bs-theme', theme === 'dark' ? 'dark' : 'light');
        const btn = $('#themeToggleBtn i');
        if (btn) {
            btn.className = theme === 'dark' ? 'fas fa-sun' : 'fas fa-moon';
        }
    }
    function toggleTheme() {
        const current = localStorage.getItem(THEME_KEY) || 'light';
        const next = current === 'dark' ? 'light' : 'dark';
        localStorage.setItem(THEME_KEY, next);
        applyTheme(next);
    }

    /* ========================================================== Sidebar */
    function setSidebarCollapsed(collapsed) {
        const sb = $('#erpSidebar');
        if (!sb) return;
        sb.classList.toggle('collapsed', !!collapsed);
        const icon = $('#sidebarCollapseBtn i');
        if (icon) {
            icon.className = collapsed ? 'fas fa-chevron-right' : 'fas fa-chevron-left';
        }
        try { localStorage.setItem(SIDEBAR_KEY, collapsed ? '1' : '0'); } catch {}
    }
    function toggleMobileSidebar(force) {
        const sb = $('#erpSidebar');
        const ov = $('#erpMobileOverlay');
        if (!sb || !ov) return;
        const willOpen = force === undefined ? !sb.classList.contains('mobile-open') : !!force;
        sb.classList.toggle('mobile-open', willOpen);
        ov.classList.toggle('active', willOpen);
        document.body.style.overflow = willOpen ? 'hidden' : '';
    }

    /* ========================================================== Toasts */
    function showToast(message, type = 'info', delay = 4000) {
        const container = $('#erpToastContainer');
        if (!container || !message) return;
        const icons = {
            success: 'fa-check-circle',
            danger: 'fa-circle-xmark',
            warning: 'fa-triangle-exclamation',
            info: 'fa-circle-info'
        };
        const colors = {
            success: 'text-bg-success',
            danger: 'text-bg-danger',
            warning: 'text-bg-warning',
            info: 'text-bg-primary'
        };
        const id = 'toast-' + Math.random().toString(36).slice(2, 8);
        const html = `
            <div class="toast align-items-center ${colors[type] || colors.info} border-0 erp-fade-in" role="alert" id="${id}" data-bs-delay="${delay}">
                <div class="d-flex">
                    <div class="toast-body d-flex align-items-center gap-2">
                        <i class="fas ${icons[type] || icons.info}"></i>
                        <span>${escapeHtml(message)}</span>
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>`;
        container.insertAdjacentHTML('beforeend', html);
        const el = $('#' + id);
        if (window.bootstrap && el) {
            const t = new bootstrap.Toast(el);
            t.show();
            el.addEventListener('hidden.bs.toast', () => el.remove());
        }
    }
    function escapeHtml(s) {
        return String(s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    /* ========================================================== DataTables defaults */
    function initDataTables() {
        if (!window.jQuery || !window.jQuery.fn || !window.jQuery.fn.DataTable) return;
        const $tables = jQuery('.datatable, table.dataTable-init');
        if (!$tables.length) return;

        $tables.each(function () {
            const $t = jQuery(this);
            if ($t.hasClass('dataTable')) return; // already
            $t.DataTable({
                responsive: true,
                pageLength: 10,
                lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, 'Όλες']],
                order: [],
                language: {
                    sProcessing: 'Επεξεργασία...',
                    sLengthMenu: 'Εμφάνιση _MENU_ καταχωρήσεων',
                    sZeroRecords: 'Δεν βρέθηκαν αποτελέσματα',
                    sInfo: 'Εμφάνιση _START_ έως _END_ από _TOTAL_ καταχωρήσεις',
                    sInfoEmpty: 'Εμφάνιση 0 έως 0 από 0 καταχωρήσεις',
                    sInfoFiltered: '(φιλτραρισμένες από _MAX_ συνολικά)',
                    sSearch: 'Αναζήτηση:',
                    sEmptyTable: 'Δεν υπάρχουν δεδομένα',
                    oPaginate: {
                        sFirst: 'Πρώτη',
                        sPrevious: '‹',
                        sNext: '›',
                        sLast: 'Τελευταία'
                    }
                }
            });
        });
    }

    /* ========================================================== Bootstrap tooltips */
    function initTooltips() {
        if (!window.bootstrap) return;
        $$('[data-bs-toggle="tooltip"]').forEach(el => {
            try { new bootstrap.Tooltip(el); } catch {}
        });
    }

    /* ========================================================== Confirm forms */
    function initConfirmForms() {
        $$('form[data-confirm]').forEach(form => {
            form.addEventListener('submit', (e) => {
                const msg = form.getAttribute('data-confirm') || 'Είστε σίγουροι;';
                if (!window.confirm(msg)) {
                    e.preventDefault();
                    e.stopPropagation();
                }
            });
        });
    }

    /* ========================================================== Search modal */
    function initSearch() {
        const input = $('#globalSearchInput');
        if (input) {
            const modalEl = $('#globalSearchModal');
            if (modalEl) {
                modalEl.addEventListener('shown.bs.modal', () => input.focus());
                input.addEventListener('keydown', (e) => {
                    if (e.key === 'Enter' && input.value.trim()) {
                        // Naive default: send to formulas index search
                        window.location.href = '/Formulas?searchString=' + encodeURIComponent(input.value.trim());
                    }
                });
            }
        }
        // Ctrl/Cmd + K
        document.addEventListener('keydown', (e) => {
            const isCmd = e.ctrlKey || e.metaKey;
            if (isCmd && (e.key === 'k' || e.key === 'K')) {
                e.preventDefault();
                const modalEl = $('#globalSearchModal');
                if (modalEl && window.bootstrap) {
                    bootstrap.Modal.getOrCreateInstance(modalEl).show();
                }
            }
        });
    }

    /* ========================================================== Init */
    document.addEventListener('DOMContentLoaded', () => {
        // Theme
        const savedTheme = localStorage.getItem(THEME_KEY) || 'light';
        applyTheme(savedTheme);
        const tBtn = $('#themeToggleBtn');
        if (tBtn) tBtn.addEventListener('click', toggleTheme);

        // Sidebar collapse (desktop)
        const sbCollapsed = localStorage.getItem(SIDEBAR_KEY) === '1';
        if (sbCollapsed && window.innerWidth >= 992) setSidebarCollapsed(true);
        const collapseBtn = $('#sidebarCollapseBtn');
        if (collapseBtn) {
            collapseBtn.addEventListener('click', () => {
                const sb = $('#erpSidebar');
                setSidebarCollapsed(!sb.classList.contains('collapsed'));
            });
        }

        // Mobile menu
        const mobileBtn = $('#mobileMenuToggle');
        if (mobileBtn) mobileBtn.addEventListener('click', () => toggleMobileSidebar());
        const overlay = $('#erpMobileOverlay');
        if (overlay) overlay.addEventListener('click', () => toggleMobileSidebar(false));

        // Close mobile sidebar on link click
        $$('.erp-sidebar-link').forEach(l => {
            l.addEventListener('click', () => {
                if (window.innerWidth < 992) toggleMobileSidebar(false);
            });
        });

        // Resize handler
        let resizeTO;
        window.addEventListener('resize', () => {
            clearTimeout(resizeTO);
            resizeTO = setTimeout(() => {
                if (window.innerWidth >= 992) toggleMobileSidebar(false);
            }, 150);
        });

        // Components
        initDataTables();
        initTooltips();
        initConfirmForms();
        initSearch();

        // Initial server-side toast
        if (window.__erpInitialToast) {
            showToast(window.__erpInitialToast.message, window.__erpInitialToast.type || 'info');
            delete window.__erpInitialToast;
        }
    });

    /* ========================================================== Public API */
    window.ErpUI = {
        toast: showToast,
        toggleTheme,
        setSidebarCollapsed,
        openSearch: () => {
            const m = $('#globalSearchModal');
            if (m && window.bootstrap) bootstrap.Modal.getOrCreateInstance(m).show();
        }
    };
})();
