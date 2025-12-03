// Chart.js Helper for BrightEnroll_DES Dashboard
window.chartHelper = {
    charts: {},

    // Initialize or update a line chart
    initLineChart: function (canvasId, data) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element with id '${canvasId}' not found`);
            return;
        }

        const ctx = canvas.getContext('2d');

        // Destroy existing chart if it exists
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        this.charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: data.labels || [],
                datasets: data.datasets || []
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: data.showLegend !== false,
                        position: 'top',
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
                                if (data.isCurrency) {
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
                },
                interaction: {
                    mode: 'nearest',
                    axis: 'x',
                    intersect: false
                }
            }
        });
    },

    // Initialize or update a bar chart
    initBarChart: function (canvasId, data) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element with id '${canvasId}' not found`);
            return;
        }

        const ctx = canvas.getContext('2d');

        // Destroy existing chart if it exists
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        this.charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: data.labels || [],
                datasets: data.datasets || []
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: data.showLegend !== false,
                        position: 'top',
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
                                if (data.isCurrency) {
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
            }
        });
    },

    // Initialize or update a pie/doughnut chart
    initPieChart: function (canvasId, data, isDoughnut = false) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) {
            console.error(`Canvas element with id '${canvasId}' not found`);
            return;
        }

        const ctx = canvas.getContext('2d');

        // Destroy existing chart if it exists
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
        }

        this.charts[canvasId] = new Chart(ctx, {
            type: isDoughnut ? 'doughnut' : 'pie',
            data: {
                labels: data.labels || [],
                datasets: [{
                    data: data.values || [],
                    backgroundColor: data.colors || [],
                    borderWidth: 2,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: data.showLegend !== false,
                        position: data.legendPosition || 'bottom',
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
            }
        });
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

