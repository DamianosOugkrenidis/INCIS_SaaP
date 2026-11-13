// Author: Damianos Pythagoras Ougkrenidis
// Formula Version Control JavaScript

(function () {
    'use strict';

    // Version History Timeline
    class VersionTimeline {
        constructor(container) {
            this.container = container;
            this.versions = [];
        }

        async loadVersions(formulaId) {
            try {
                const response = await fetch(`/api/formulas/${formulaId}/versions`);
                const data = await response.json();
                
                if (data.success) {
                    this.versions = data.data;
                    this.render();
                }
            } catch (error) {
                console.error('Error loading versions:', error);
            }
        }

        render() {
            const html = this.versions.map(version => `
                <div class="version-item" data-id="${version.id}">
                    <div class="version-badge">v${version.versionNumber}</div>
                    <div class="version-details">
                        <h5>${version.changeDescription || 'No description'}</h5>
                        <p><strong>Type:</strong> ${version.changeType}</p>
                        <p><strong>By:</strong> ${version.createdBy} on ${new Date(version.createdAt).toLocaleString()}</p>
                        <div class="version-actions">
                            <button class="btn btn-sm btn-primary view-btn" data-id="${version.id}">
                                <i class="fas fa-eye"></i> View
                            </button>
                            ${version.isCurrentVersion ? 
                                '<span class="badge bg-success">Current</span>' : 
                                `<button class="btn btn-sm btn-warning restore-btn" data-id="${version.id}">
                                    <i class="fas fa-undo"></i> Restore
                                </button>`
                            }
                        </div>
                    </div>
                </div>
            `).join('');

            this.container.innerHTML = html;
            this.attachEventListeners();
        }

        attachEventListeners() {
            this.container.querySelectorAll('.view-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const versionId = e.currentTarget.dataset.id;
                    window.location.href = `/FormulaVersion/Details/${versionId}`;
                });
            });

            this.container.querySelectorAll('.restore-btn').forEach(btn => {
                btn.addEventListener('click', (e) => {
                    const versionId = e.currentTarget.dataset.id;
                    this.confirmRestore(versionId);
                });
            });
        }

        confirmRestore(versionId) {
            if (confirm('Are you sure you want to restore this version? This will create a new version.')) {
                this.restoreVersion(versionId);
            }
        }

        async restoreVersion(versionId) {
            try {
                const formulaId = this.container.dataset.formulaId;
                const response = await fetch(`/api/formulas/${formulaId}/versions/${versionId}/restore`, {
                    method: 'POST'
                });
                
                const data = await response.json();
                
                if (data.success) {
                    alert('Version restored successfully');
                    location.reload();
                } else {
                    alert('Failed to restore version');
                }
            } catch (error) {
                console.error('Error restoring version:', error);
                alert('An error occurred');
            }
        }
    }

    // Version Comparison
    class VersionComparison {
        constructor(container) {
            this.container = container;
        }

        async compare(sourceVersionId, targetVersionId) {
            try {
                const response = await fetch(
                    `/api/formulas/1/versions/compare?sourceVersionId=${sourceVersionId}&targetVersionId=${targetVersionId}`
                );
                const data = await response.json();
                
                if (data.success) {
                    this.renderComparison(data.data);
                }
            } catch (error) {
                console.error('Error comparing versions:', error);
            }
        }

        renderComparison(comparison) {
            const differences = JSON.parse(comparison.differencesJson);
            
            const html = `
                <div class="comparison-result">
                    <h4>Comparison Summary</h4>
                    <p>${comparison.summary}</p>
                    <div class="differences">
                        ${this.renderDifferences(differences)}
                    </div>
                </div>
            `;

            this.container.innerHTML = html;
        }

        renderDifferences(differences) {
            if (differences.length === 0) {
                return '<p>No differences found</p>';
            }

            return differences.map(diff => `
                <div class="diff-item">
                    <strong>${diff.field}:</strong>
                    <span class="old-value">${diff.oldValue}</span>
                    <i class="fas fa-arrow-right"></i>
                    <span class="new-value">${diff.newValue}</span>
                </div>
            `).join('');
        }
    }

    // Initialize on page load
    document.addEventListener('DOMContentLoaded', function() {
        const timelineContainer = document.getElementById('version-timeline');
        if (timelineContainer) {
            const timeline = new VersionTimeline(timelineContainer);
            const formulaId = timelineContainer.dataset.formulaId;
            timeline.loadVersions(formulaId);
        }

        const comparisonContainer = document.getElementById('version-comparison');
        if (comparisonContainer) {
            const comparison = new VersionComparison(comparisonContainer);
            // Comparison will be triggered by user action
            window.formulaVersionComparison = comparison;
        }
    });

})();
