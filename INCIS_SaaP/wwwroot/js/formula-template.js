// Author: Damianos Pythagoras Ougkrenidis
// Formula Template Management JavaScript

(function () {
    'use strict';

    class FormulaTemplateManager {
        constructor() {
            this.currentTemplate = null;
            this.categories = [];
        }

        async loadTemplateCategories() {
            try {
                const response = await fetch('/api/formula-templates/categories');
                const data = await response.json();
                if (data.success) {
                    this.categories = data.data;
                    this.renderCategories();
                }
            } catch (error) {
                console.error('Error loading categories:', error);
            }
        }

        async searchTemplates(searchTerm) {
            try {
                const response = await fetch(`/api/formula-templates/search?searchTerm=${encodeURIComponent(searchTerm)}`);
                const data = await response.json();
                if (data.success) {
                    this.renderTemplateList(data.data);
                }
            } catch (error) {
                console.error('Error searching templates:', error);
            }
        }

        async rateTemplate(templateId, rating) {
            try {
                const response = await fetch(`/api/formula-templates/${templateId}/rate`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ rating })
                });
                
                const data = await response.json();
                if (data.success) {
                    alert('Template rated successfully');
                    location.reload();
                }
            } catch (error) {
                console.error('Error rating template:', error);
            }
        }

        async applyTemplate(templateId, newFormulaCode, clientId) {
            try {
                const response = await fetch(`/api/formula-templates/${templateId}/apply`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ newFormulaCode, clientId })
                });
                
                const data = await response.json();
                if (data.success) {
                    alert('Template applied successfully');
                    window.location.href = `/Formulas/Details/${data.data.id}`;
                }
            } catch (error) {
                console.error('Error applying template:', error);
            }
        }

        renderTemplateList(templates) {
            const container = document.getElementById('template-list');
            if (!container) return;

            const html = templates.map(template => `
                <div class="card template-card mb-3" data-id="${template.id}">
                    <div class="card-body">
                        <h5 class="card-title">${template.templateName}</h5>
                        <p class="card-text">${template.description || 'No description'}</p>
                        <div class="template-meta">
                            <span><i class="fas fa-star"></i> ${template.rating?.toFixed(1) || 'N/A'}</span>
                            <span><i class="fas fa-users"></i> Used ${template.usageCount} times</span>
                        </div>
                        <button class="btn btn-primary btn-sm apply-template-btn" data-id="${template.id}">
                            <i class="fas fa-plus"></i> Apply
                        </button>
                    </div>
                </div>
            `).join('');

            container.innerHTML = html;
            this.attachEventListeners();
        }

        renderCategories() {
            // Category tree rendering logic
        }

        attachEventListeners() {
            document.querySelectorAll('.apply-template-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const templateId = e.currentTarget.dataset.id;
                    this.showApplyModal(templateId);
                });
            });
        }

        showApplyModal(templateId) {
            // Show modal for applying template
            const modal = document.getElementById('applyTemplateModal');
            if (modal) {
                modal.dataset.templateId = templateId;
                // Bootstrap modal show
            }
        }
    }

    // Initialize
    window.formulTemplateManager = new FormulaTemplateManager();

})();
