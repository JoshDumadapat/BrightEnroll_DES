// Dashboard Charts Helper - Simplified functions for Dashboard and Reports
window.dashboardCharts = {
    charts: {},

    // Render line chart
    renderLineChart: function (canvasId, labels, datasets, options) {
        try {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                console.warn(`Canvas element with id '${canvasId}' not found`);
                return false;
            }

            const ctx = canvas.getContext('2d');

            // Destroy existing chart if it exists
            if (this.charts[canvasId]) {
                this.charts[canvasId].destroy();
            }

            const defaultOptions = {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: options?.showLegend !== false,
                        position: options?.legendPosition || 'top',
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                    }
                },
                scales: {
                    y: {
                        beginAtZero: options?.minY == null, // Only begin at zero if minY is not specified
                        min: options?.minY ?? 0, // Start from 400K if specified, otherwise 0
                        max: options?.minY != null ? 2000000 : undefined, // Max 2M when minY is set (for income chart)
                        ticks: {
                            stepSize: options?.minY != null ? 400000 : undefined, // 400K increments when minY is set
                            callback: function (value) {
                                if (options?.isCurrency) {
                                    // Format large numbers with K and M suffixes
                                    if (value >= 1000000) {
                                        const millions = value / 1000000;
                                        // Show whole numbers without decimal (e.g., 2M instead of 2.0M)
                                        if (millions % 1 === 0) {
                                            return '₱' + millions.toFixed(0) + 'M';
                                        }
                                        return '₱' + millions.toFixed(1) + 'M';
                                    } else if (value >= 1000) {
                                        return '₱' + (value / 1000).toFixed(0) + 'K';
                                    }
                                    return '₱' + value.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
                                }
                                return value.toLocaleString();
                            }
                        },
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            maxRotation: 0,
                            minRotation: 0
                        }
                    }
                },
                interaction: {
                    mode: 'nearest',
                    axis: 'x',
                    intersect: false
                },
                elements: {
                    line: {
                        tension: 0.4, // Smooth curves
                        borderWidth: 2,
                        fill: false
                    },
                    point: {
                        radius: 4,
                        hoverRadius: 6,
                        borderWidth: 2
                    }
                }
            };

            const mergedOptions = { ...defaultOptions, ...(options || {}) };

            // Ensure datasets have proper structure for Chart.js
            const formattedDatasets = (datasets || []).map(ds => ({
                label: ds.label || '',
                data: ds.data || [],
                borderColor: ds.borderColor || '#3b82f6',
                backgroundColor: ds.backgroundColor || 'rgba(59, 130, 246, 0.1)',
                tension: ds.tension !== undefined ? ds.tension : 0.4,
                fill: ds.fill !== undefined ? ds.fill : false,
                pointRadius: ds.pointRadius !== undefined ? ds.pointRadius : 4,
                pointHoverRadius: ds.pointHoverRadius !== undefined ? ds.pointHoverRadius : 6,
                borderWidth: ds.borderWidth !== undefined ? ds.borderWidth : 2,
                showLine: true // Explicitly enable line rendering
            }));

            this.charts[canvasId] = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: labels || [],
                    datasets: formattedDatasets
                },
                options: mergedOptions
            });

            return true;
        } catch (error) {
            console.error(`Error rendering line chart ${canvasId}:`, error);
            return false;
        }
    },

    // Render bar chart
    renderBarChart: function (canvasId, labels, datasets, options) {
        try {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                console.warn(`Canvas element with id '${canvasId}' not found`);
                return false;
            }

            const ctx = canvas.getContext('2d');

            // Destroy existing chart if it exists
            if (this.charts[canvasId]) {
                this.charts[canvasId].destroy();
            }

            const defaultOptions = {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: options?.showLegend !== false,
                        position: options?.legendPosition || 'top',
                    },
                    tooltip: {
                        mode: 'index',
                        intersect: false,
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                if (options?.isCurrency) {
                                    return '₱' + value.toLocaleString('en-US', { minimumFractionDigits: 0, maximumFractionDigits: 0 });
                                }
                                return value.toLocaleString();
                            }
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        }
                    }
                }
            };

            const mergedOptions = { ...defaultOptions, ...(options || {}) };

            this.charts[canvasId] = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: labels || [],
                    datasets: datasets || []
                },
                options: mergedOptions
            });

            return true;
        } catch (error) {
            console.error(`Error rendering bar chart ${canvasId}:`, error);
            return false;
        }
    },

    // Render pie/doughnut chart
    renderPieChart: function (canvasId, labels, values, colors, isDoughnut, options) {
        try {
            const canvas = document.getElementById(canvasId);
            if (!canvas) {
                console.warn(`Canvas element with id '${canvasId}' not found`);
                return false;
            }

            const ctx = canvas.getContext('2d');

            // Destroy existing chart if it exists
            if (this.charts[canvasId]) {
                this.charts[canvasId].destroy();
            }

            const defaultOptions = {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: options?.showLegend !== false,
                        position: options?.legendPosition || 'bottom',
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                const label = context.label || '';
                                const value = context.parsed || 0;
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = total > 0 ? ((value / total) * 100).toFixed(1) : 0;
                                return `${label}: ${value.toLocaleString()} (${percentage}%)`;
                            }
                        }
                    }
                }
            };

            const mergedOptions = { ...defaultOptions, ...(options || {}) };

            this.charts[canvasId] = new Chart(ctx, {
                type: isDoughnut ? 'doughnut' : 'pie',
                data: {
                    labels: labels || [],
                    datasets: [{
                        data: values || [],
                        backgroundColor: colors || [],
                        borderWidth: 2,
                        borderColor: '#ffffff'
                    }]
                },
                options: mergedOptions
            });

            return true;
        } catch (error) {
            console.error(`Error rendering pie chart ${canvasId}:`, error);
            return false;
        }
    },

    // Destroy a specific chart
    destroyChart: function (canvasId) {
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
            delete this.charts[canvasId];
        }
    },

    // Destroy all charts
    destroyAllCharts: function () {
        Object.keys(this.charts).forEach(canvasId => {
            this.destroyChart(canvasId);
        });
    },

    // Check if all canvas elements exist
    checkCanvasesExist: function () {
        const canvasIds = ['incomeChart', 'studentTypeChart', 'enrollmentByGradeChart', 'paymentStatusChart'];
        return canvasIds.every(id => document.getElementById(id) !== null);
    }
};

