/**
 * CosIng Autocomplete & Regulatory Check Module
 * ===============================================
 * Χρήση σε όλες τις φόρμες δημιουργίας/επεξεργασίας φόρμουλας
 * 
 * API Endpoints:
 *   GET /api/cosing/ingredients/search?q=&limit=
 *   GET /api/cosing/check/prohibited?q=
 *   GET /api/cosing/check/restricted?q=
 *   GET /api/cosing/check/full?q=
 *   POST /api/cosing/check/batch
 *
 * Χρήση:
 *   <input type="text" class="cosing-autocomplete" data-target="#hiddenField" />
 *   
 *   CosIng.init();  // Αρχικοποίηση autocomplete σε όλα τα .cosing-autocomplete
 *   CosIng.checkIngredient('GLYCERIN');  // Regulatory check
 *   CosIng.batchCheck(['GLYCERIN', 'AQUA']);  // Batch check
 */
const CosIng = (function () {
    'use strict';

    const API_BASE = '/api/cosing';
    let debounceTimer = null;

    // ============================================================
    // AUTOCOMPLETE
    // ============================================================

    /**
     * Αρχικοποίηση autocomplete σε όλα τα inputs με class .cosing-autocomplete
     */
    function init() {
        document.querySelectorAll('.cosing-autocomplete').forEach(input => {
            setupAutocomplete(input);
        });
        console.log('[CosIng] Autocomplete initialized');
    }

    function setupAutocomplete(input) {
        // Δημιουργία dropdown container
        const wrapper = document.createElement('div');
        wrapper.className = 'cosing-wrapper position-relative';
        input.parentNode.insertBefore(wrapper, input);
        wrapper.appendChild(input);

        const dropdown = document.createElement('div');
        dropdown.className = 'cosing-dropdown list-group position-absolute w-100 shadow-sm';
        dropdown.style.cssText = 'z-index:1050; max-height:300px; overflow-y:auto; display:none;';
        wrapper.appendChild(dropdown);

        // Status indicator
        const status = document.createElement('small');
        status.className = 'cosing-status text-muted d-block mt-1';
        status.style.display = 'none';
        wrapper.appendChild(status);

        // Event listeners
        input.addEventListener('input', function () {
            const q = this.value.trim();
            if (q.length < 2) {
                dropdown.style.display = 'none';
                return;
            }
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => fetchIngredients(q, dropdown, input, status), 300);
        });

        input.addEventListener('blur', function () {
            setTimeout(() => { dropdown.style.display = 'none'; }, 200);
        });

        input.addEventListener('focus', function () {
            if (dropdown.children.length > 0 && this.value.length >= 2) {
                dropdown.style.display = 'block';
            }
        });
    }

    async function fetchIngredients(query, dropdown, input, status) {
        try {
            const response = await fetch(`${API_BASE}/ingredients/search?q=${encodeURIComponent(query)}&limit=15`);
            if (!response.ok) return;

            const data = await response.json();
            renderDropdown(data, dropdown, input, status);
        } catch (err) {
            console.error('[CosIng] Search error:', err);
        }
    }

    function renderDropdown(items, dropdown, input, status) {
        dropdown.innerHTML = '';

        if (items.length === 0) {
            dropdown.innerHTML = '<div class="list-group-item text-muted small">Δεν βρέθηκαν αποτελέσματα</div>';
            dropdown.style.display = 'block';
            return;
        }

        items.forEach(item => {
            const el = document.createElement('a');
            el.href = '#';
            el.className = 'list-group-item list-group-item-action py-2 px-3';
            el.innerHTML = `
                <div class="fw-semibold">${escapeHtml(item.inciName)}</div>
                <small class="text-muted">
                    ${item.casNumber ? 'CAS: ' + escapeHtml(item.casNumber) : ''}
                    ${item.function ? ' | ' + escapeHtml(item.function.substring(0, 60)) : ''}
                </small>
            `;

            el.addEventListener('mousedown', function (e) {
                e.preventDefault();
                selectIngredient(item, input, dropdown, status);
            });

            dropdown.appendChild(el);
        });

        dropdown.style.display = 'block';
    }

    function selectIngredient(item, input, dropdown, status) {
        input.value = item.inciName;
        dropdown.style.display = 'none';

        // Set hidden fields if configured
        const targetId = input.dataset.target;
        if (targetId) {
            const target = document.querySelector(targetId);
            if (target) target.value = item.id;
        }

        // Set CAS field if configured
        const casTarget = input.dataset.casTarget;
        if (casTarget) {
            const casEl = document.querySelector(casTarget);
            if (casEl) casEl.value = item.casNumber || '';
        }

        // Set EC field if configured
        const ecTarget = input.dataset.ecTarget;
        if (ecTarget) {
            const ecEl = document.querySelector(ecTarget);
            if (ecEl) ecEl.value = item.ecNumber || '';
        }

        // Set Function field if configured
        const funcTarget = input.dataset.functionTarget;
        if (funcTarget) {
            const funcEl = document.querySelector(funcTarget);
            if (funcEl) funcEl.value = item.function || '';
        }

        // Auto-run regulatory check
        checkIngredient(item.inciName).then(result => {
            showStatus(status, result);
            // Dispatch custom event for other modules to listen
            input.dispatchEvent(new CustomEvent('cosing:selected', {
                detail: { ingredient: item, regulatoryCheck: result },
                bubbles: true
            }));
        });
    }

    function showStatus(status, result) {
        if (!status) return;
        status.style.display = 'block';

        if (result.isProhibited) {
            status.className = 'cosing-status text-danger fw-bold d-block mt-1';
            status.innerHTML = '<i class="fas fa-ban"></i> ΑΠΑΓΟΡΕΥΜΕΝΗ ΟΥΣΙΑ (Annex II) - Δεν επιτρέπεται σε καλλυντικά!';
        } else if (result.isRestricted) {
            status.className = 'cosing-status text-warning fw-bold d-block mt-1';
            const maxConc = result.maxConcentration ? ` | Max: ${result.maxConcentration}` : '';
            status.innerHTML = `<i class="fas fa-exclamation-triangle"></i> ΟΥΣΙΑ ΜΕ ΠΕΡΙΟΡΙΣΜΟΥΣ (Annex III)${maxConc}`;
        } else {
            status.className = 'cosing-status text-success d-block mt-1';
            status.innerHTML = '<i class="fas fa-check-circle"></i> OK - Χωρίς κανονιστικούς περιορισμούς';
        }
    }

    // ============================================================
    // REGULATORY CHECKS
    // ============================================================

    /**
     * Έλεγχος μεμονωμένου ingredient
     */
    async function checkIngredient(name) {
        try {
            const response = await fetch(`${API_BASE}/check/full?q=${encodeURIComponent(name)}`);
            if (!response.ok) return { isProhibited: false, isRestricted: false };

            const data = await response.json();
            return {
                isProhibited: data.summary.isProhibited,
                isRestricted: data.summary.isRestricted,
                hasCMR: data.summary.hasCMR,
                maxConcentration: data.restricted?.[0]?.maxConcentration,
                warnings: data.restricted?.[0]?.warnings,
                details: data
            };
        } catch (err) {
            console.error('[CosIng] Check error:', err);
            return { isProhibited: false, isRestricted: false };
        }
    }

    /**
     * Batch check πολλαπλών ingredients (π.χ. κατά αποθήκευση φόρμουλας)
     */
    async function batchCheck(inciNames) {
        try {
            const response = await fetch(`${API_BASE}/check/batch`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ inciNames })
            });
            if (!response.ok) return null;
            return await response.json();
        } catch (err) {
            console.error('[CosIng] Batch check error:', err);
            return null;
        }
    }

    /**
     * Εμφάνιση regulatory alert modal (για χρήση κατά αποθήκευση φόρμουλας)
     */
    function showRegulatoryAlert(batchResult) {
        if (!batchResult) return;

        const prohibited = batchResult.results.filter(r => r.isProhibited);
        const restricted = batchResult.results.filter(r => r.isRestricted);

        if (prohibited.length === 0 && restricted.length === 0) return;

        let html = '<div class="alert alert-danger" role="alert">';
        html += '<h5><i class="fas fa-exclamation-circle"></i> Κανονιστικοί Έλεγχοι CosIng</h5>';

        if (prohibited.length > 0) {
            html += '<div class="mb-2"><strong class="text-danger">ΑΠΑΓΟΡΕΥΜΕΝΕΣ ΟΥΣΙΕΣ (Annex II):</strong><ul>';
            prohibited.forEach(p => {
                html += `<li><strong>${escapeHtml(p.inciName)}</strong></li>`;
            });
            html += '</ul></div>';
        }

        if (restricted.length > 0) {
            html += '<div><strong class="text-warning">ΟΥΣΙΕΣ ΜΕ ΠΕΡΙΟΡΙΣΜΟΥΣ (Annex III):</strong><ul>';
            restricted.forEach(r => {
                html += `<li><strong>${escapeHtml(r.inciName)}</strong>`;
                if (r.maxConcentration) html += ` — Max: ${escapeHtml(r.maxConcentration)}`;
                html += '</li>';
            });
            html += '</ul></div>';
        }

        html += '</div>';

        // Show in modal or inline
        const container = document.getElementById('cosing-alerts');
        if (container) {
            container.innerHTML = html;
            container.style.display = 'block';
        } else {
            // Fallback: create alert at top of form
            const form = document.querySelector('form');
            if (form) {
                const alertDiv = document.createElement('div');
                alertDiv.id = 'cosing-alerts';
                alertDiv.innerHTML = html;
                form.prepend(alertDiv);
            }
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    function escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    // ============================================================
    // PUBLIC API
    // ============================================================
    return {
        init,
        checkIngredient,
        batchCheck,
        showRegulatoryAlert,
        setupAutocomplete
    };

})();

// Auto-initialize when DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    CosIng.init();
});
