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
                        beginAtZero: true,
                        ticks: {
                            callback: function (value) {
                                if (options?.isCurrency) {
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

            this.charts[canvasId] = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: labels || [],
                    datasets: datasets || []
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
    }
};

