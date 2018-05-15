//
// NReco PivotData Pivot Chart Plugin (renders pivot data with C3.js)
// @author Vitaliy Fedorchenko (original plugin version), Elyor Latipov (C3js integration)
//  

(function ($) {

    function NRecoPivotChart(element, options) {
        this.element = element;
        this.options = options;

        init(this);
    }

    function init(pvtChart) {
        var o = pvtChart.options;

        if ((typeof pvtChart[o.chartType]) == "function") {
            pvtChart[o.chartType].apply(pvtChart, []);
        } else {
            $.error("Unknown chart type: " + o.chartType);
        }

    }

    function ifnull(o, nullValue) {

        if (o instanceof Array) {
            var arr = [];
            for (var i = 0; i < o.length; i++)
                arr.push(ifnull(o[i], nullValue));
            return arr;
        }

        return o != null ? o : nullValue;
    }

    function valOrFirstElem(o) {
        if (o instanceof Array)
            return o[0];

        return o;
    }

    function getMeasureValuesArray(values) {
        var res = [];

        for (var i = 0; i < values.length; i++)
            res.push(ifnull(valOrFirstElem(values[i]), 0));

        return res;
    }

    NRecoPivotChart.prototype.getChartData = function (totalsOnly) {

        var pivotData = this.options.pivotData;
        var chartData = {
            labels: [],
            series: []
        };
        var addLabels = function (dimKeys, dims) {

            for (var i = 0; i < dimKeys.length; i++)
                chartData.labels.push(dimKeys[i].join(" "));

            if (dims)
                chartData.axisLabel = dims.join(", ");
        };

        if (pivotData.MeasureLabels.length > 1) {

            // handle multiple measures in a special way
            if (pivotData.RowKeys.length > 0) {
                addLabels(pivotData.RowKeys, pivotData.Rows);
                for (var i = 0; i < pivotData.MeasureLabels.length; i++) {
                    var seriesData = [];
                    for (var j = 0; j < pivotData.RowTotals.length; j++) {
                        seriesData.push(ifnull(pivotData.RowTotals[j][i], 0));
                    }
                    seriesData.name = pivotData.MeasureLabels[i];
                    chartData.series.push(seriesData);
                }
            } else if (pivotData.ColumnKeys.length > 0) {
                addLabels(pivotData.ColumnKeys, pivotData.Columns);
                for (var i = 0; i < pivotData.MeasureLabels.length; i++) {
                    var seriesData = [];
                    for (var j = 0; j < pivotData.ColumnTotals.length; j++) {
                        seriesData.push(ifnull(pivotData.ColumnTotals[j][i], 0));
                    }
                    seriesData.name = pivotData.MeasureLabels[i];
                    chartData.series.push(seriesData);
                }
            } else {
                chartData.labels = pivotData.MeasureLabels;
                chartData.series.push(pivotData.GrandTotal);
            }

            return chartData;
        }

        if (pivotData.RowKeys.length > 0 && pivotData.ColumnKeys.length > 0 && !totalsOnly) {
            if (pivotData.ColumnKeys.length > 0) {
                addLabels(pivotData.ColumnKeys, pivotData.Columns);
            } else {
                addLabels(pivotData.RowKeys, pivotData.Rows);
            }
            for (var r = 0; r < pivotData.Values.length; r++) {
                var row = pivotData.Values[r];
                var rowData = getMeasureValuesArray(row);
                rowData.name = pivotData.RowKeys[r].join(" ");
                chartData.series.push(rowData);
            }
        } else {
            if (pivotData.RowTotals.length > 0) {
                addLabels(pivotData.RowKeys, pivotData.Rows);
                chartData.series.push(getMeasureValuesArray(pivotData.RowTotals));
            } else if (pivotData.ColumnTotals.length > 0) {
                addLabels(pivotData.ColumnKeys, pivotData.Columns);
                chartData.series.push(getMeasureValuesArray(pivotData.ColumnTotals));
            } else {
                chartData.labels = ["Grand Total"];
                chartData.series.push([ifnull(pivotData.GrandTotal, 0)]);
            }
        }

        return chartData;
    };

    NRecoPivotChart.prototype.bar = function () {
        var pivotData = this.options.pivotData;
        var chartData = this.getChartData();
        var chartOpts = $.extend({}, this.options.chartOptions);

        var labels = chartData.labels;
        var series = chartData.series;
        var items = [];
        var axisX = labels;
        var xLabel = pivotData.Columns.join();
        var yLabel = pivotData.MeasureLabels.join();

        for (var i = 0; i < series.length; i++) {
            var item = [];

            item[0] = series[i].name;

            if (series[i] && series[i].length > 0) {
                for (var j = 0; j < series[i].length; j++) {
                    item[j + 1] = series[i][j];
                }
            }

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    type: "bar"
                },
                axis: {
                    x: {
                        type: "category",
                        categories: axisX,
                        tick: {
                            rotate: -75,
                            multiline: true
                        },
                        height: 100,
                        label: {
                            text: xLabel,
                            position: "outer-right"
                        }
                    },
                    y: {
                        label: {
                            text: yLabel,
                            position: "outer-top"
                        }
                    }
                }
            });
        } else {
            this.chart.load({
                columns: items
            });
        }

    };

    NRecoPivotChart.prototype.stackedBar = function () {
        var pivotData = this.options.pivotData;
        var chartData = this.getChartData();
        var chartOpts = $.extend({}, this.options.chartOptions);

        var items = [];
        var labels = chartData.labels;
        var series = chartData.series;
        var groups = [];
        var axisX = labels;
        var xLabel = pivotData.Columns.join();
        var yLabel = pivotData.MeasureLabels.join();

        for (var i = 0; i < series.length; i++) {
            var item = [];

            item[0] = series[i].name;
            groups.push(series[i].name);

            if (series[i] && series[i].length > 0) {
                for (var j = 0; j < series[i].length; j++) {
                    item[j + 1] = series[i][j];
                }
            }

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    type: "bar",
                    groups: [
                        groups
                    ]
                },
                grid: {
                    y: {
                        lines: [{ value: 0 }]
                    }
                },
                axis: {
                    x: {
                        type: "category",
                        categories: axisX,
                        tick: {
                            rotate: -75,
                            multiline: true
                        },
                        height: 100,
                        label: {
                            text: xLabel,
                            position: "outer-right"
                        }
                    },
                    y: {
                        label: {
                            text: yLabel,
                            position: "outer-top"
                        }
                    }
                }
            });
        } else {
            this.chart.groups([
                groups
            ]);
            this.chart.load({
                columns: items
            });
        }

    };

    NRecoPivotChart.prototype.horizontalBar = function () {
        var pivotData = this.options.pivotData;
        var chartData = this.getChartData();
        var chartOpts = $.extend({}, this.options.chartOptions);

        var labels = chartData.labels;
        var series = chartData.series;
        var items = [];
        var axisX = labels;
        var xLabel = pivotData.Columns.join();
        var yLabel = pivotData.MeasureLabels.join();

        for (var i = 0; i < series.length; i++) {
            var item = [];

            item[0] = series[i].name;

            if (series[i] && series[i].length > 0) {
                for (var j = 0; j < series[i].length; j++) {
                    item[j + 1] = series[i][j];
                }
            }

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    type: "bar"
                },
                axis: {
                    rotated: true,
                    x: {
                        type: "category",
                        categories: axisX,
                        tick: {
                            rotate: -75,
                            multiline: true
                        },
                        height: 100,
                        label: {
                            text: xLabel,
                            position: "outer-right"
                        }
                    },
                    y: {
                        label: {
                            text: yLabel,
                            position: "outer-top"
                        }
                    }
                }
            });
        } else {
            this.chart.load({
                columns: items
            });
        }
    };

    NRecoPivotChart.prototype.horizontalStackedBar = function () {
        var pivotData = this.options.pivotData;
        var chartData = this.getChartData();
        var chartOpts = $.extend({}, this.options.chartOptions);

        var items = [];
        var labels = chartData.labels;
        var series = chartData.series;
        var groups = [];
        var axisX = labels;
        var xLabel = pivotData.Columns.join();
        var yLabel = pivotData.MeasureLabels.join();

        for (var i = 0; i < series.length; i++) {
            var item = [];

            item[0] = series[i].name;
            groups.push(series[i].name);

            if (series[i] && series[i].length > 0) {
                for (var j = 0; j < series[i].length; j++) {
                    item[j + 1] = series[i][j];
                }
            }

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    type: "bar",
                    groups: [
                        groups
                    ]
                },
                grid: {
                    y: {
                        lines: [{ value: 0 }]
                    }
                },
                axis: {
                    rotated: true,
                    x: {
                        type: "category",
                        categories: axisX,
                        tick: {
                            rotate: -75,
                            multiline: true
                        },
                        height: 100,
                        label: {
                            text: xLabel,
                            position: "outer-right"
                        }
                    },
                    y: {
                        label: {
                            text: yLabel,
                            position: "outer-top"
                        }
                    }
                }
            });
        } else {
            this.chart.groups([
                groups
            ]);
            this.chart.load({
                columns: items
            });
        }

    };

    NRecoPivotChart.prototype.pie = function () {
        var pivotData = this.options.pivotData;
        var chartData = this.getChartData(true);
        var chartOpts = $.extend({}, this.options.chartOptions);

        var items = [];
        var labels = chartData.labels;
        var series = chartData.series; 

        for (var i = 0; i < series[0].length; i++) {
            var item = [];
            if (labels && i < labels.length)
                item[0] = labels[i];

            item[1] = series[0][i];

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    type: "pie"
                }
            });
        } else {
            this.chart.load({
                columns: chartData
            });
        }

    };

    NRecoPivotChart.prototype.donut = function () {
        var pivotData = this.options.pivotData;
        var chartData = this.getChartData(true);
        var chartOpts = $.extend({}, this.options.chartOptions);

        var items = [];
        var labels = chartData.labels;
        var series = chartData.series;
        var totalSum = series[0].reduce(function (a, b) { return a + b });

        for (var i = 0; i < series[0].length; i++) {
            var item = [];
            if (labels && i < labels.length)
                item[0] = labels[i];

            item[1] = series[0][i];

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    type: "donut"
                }
            });
        } else {
            this.chart.load({
                columns: chartData
            });
        }

    };

    NRecoPivotChart.prototype.line = function () {

        var pivotData = this.options.pivotData;
        var chartData = this.getChartData();
        var chartOpts = $.extend({}, this.options.chartOptions);

        var items = [];
        var labels = chartData.labels;
        var series = chartData.series;
        var axisX = labels;
        var xLabel = pivotData.Columns.join();
        var yLabel = pivotData.MeasureLabels.join();

        for (var i = 0; i < series.length; i++) {
            var item = [];

            item[0] = series[i].name;

            if (series[i] && series[i].length > 0) {
                for (var j = 0; j < series[i].length; j++) {
                    item[j + 1] = series[i][j];
                }
            }

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: { 
                    columns: items,
                    type: "line"
                },
                axis: {
                    x: {
                        type: "category",
                        categories: axisX,
                        tick: {
                            rotate: -75,
                            multiline: true
                        },
                        height: 100,
                        label: {
                            text: xLabel,
                            position: "outer-right"
                        }
                    },
                    y: {
                        label: {
                            text: yLabel,
                            position: "outer-top"
                        }
                    }
                }
            });
        } else {
            this.chart.load({
                columns: items
            });
        }


    };

    NRecoPivotChart.prototype.spline = function () {

        var pivotData = this.options.pivotData;
        var chartData = this.getChartData();
        var chartOpts = $.extend({}, this.options.chartOptions);

        var items = [];
        var labels = chartData.labels;
        var series = chartData.series;
        var axisX = labels;
        var xLabel = pivotData.Columns.join();
        var yLabel = pivotData.MeasureLabels.join();

        for (var i = 0; i < series.length; i++) {
            var item = [];

            item[0] = series[i].name;

            if (series[i] && series[i].length > 0) {
                for (var j = 0; j < series[i].length; j++) {
                    item[j + 1] = series[i][j];
                }
            }

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    type: "spline"
                },
                axis: {
                    x: {
                        type: "category",
                        categories: axisX,
                        tick: {
                            rotate: -75,
                            multiline: true
                        },
                        height: 100,
                        label: {
                            text: xLabel,
                            position: "outer-right"
                        }
                    },
                    y: {
                        label: {
                            text: yLabel,
                            position: "outer-top"
                        }
                    }
                }
            });
        } else {
            this.chart.load({
                columns: items
            });
        }

    };

    NRecoPivotChart.prototype.step = function () {

        var pivotData = this.options.pivotData;
        var chartData = this.getChartData();
        var chartOpts = $.extend({}, this.options.chartOptions);

        var items = [];
        var labels = chartData.labels;
        var series = chartData.series;
        var axisX = labels; 
        var xLabel = pivotData.Columns.join();
        var yLabel = pivotData.MeasureLabels.join();

        for (var i = 0; i < series.length; i++) {
            var item = [];

            item[0] = series[i].name;

            if (series[i] && series[i].length > 0) {
                for (var j = 0; j < series[i].length; j++) {
                    item[j + 1] = series[i][j];
                }
            }

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    type: "step"
                },
                axis: {
                    x: {
                        type: "category",
                        categories: axisX,
                        tick: {
                            rotate: -75,
                            multiline: true
                        },
                        height: 100,
                        label: {
                            text: xLabel,
                            position: "outer-right"
                        }
                    },
                    y: {
                        label: {
                            text: yLabel,
                            position: "outer-top"
                        }
                    }
                }
            });
        } else {
            this.chart.load({
                columns: items
            });
        }

    };

    NRecoPivotChart.prototype.area = function () {

        var pivotData = this.options.pivotData;
        var chartData = this.getChartData();
        var chartOpts = $.extend({}, this.options.chartOptions);

        var items = [];
        var labels = chartData.labels;
        var series = chartData.series;
        var types = [];
        var axisX = labels;
        var xLabel = pivotData.Columns.join();
        var yLabel = pivotData.MeasureLabels.join();

        for (var i = 0; i < series.length; i++) {
            var item = [];

            item[0] = series[i].name;

            types[item[0]] = "area";

            if (series[i] && series[i].length > 0) {
                for (var j = 0; j < series[i].length; j++) {
                    item[j + 1] = series[i][j];
                }
            }

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    types: types
                },
                axis: {
                    x: {
                        type: "category",
                        categories: axisX,
                        tick: {
                            rotate: -75,
                            multiline: true
                        },
                        height: 100,
                        label: {
                            text: xLabel,
                            position: "outer-right"
                        }
                    },
                    y: {
                        label: {
                            text: yLabel,
                            position: "outer-top"
                        }
                    }
                }
            });
        } else {
            this.chart.load({
                columns: items
            });
        }


    };

    NRecoPivotChart.prototype.stackedArea = function () {

        var pivotData = this.options.pivotData;
        var chartData = this.getChartData();
        var chartOpts = $.extend({}, this.options.chartOptions);

        var items = [];
        var labels = chartData.labels;
        var series = chartData.series;
        var types = [];
        var groups = [];
        var axisX = labels;
        var xLabel = pivotData.Columns.join();
        var yLabel = pivotData.MeasureLabels.join();

        for (var i = 0; i < series.length; i++) {
            var item = [];

            item[0] = series[i].name;

            groups.push(series[i].name);
            types[item[0]] = "area";

            if (series[i] && series[i].length > 0) {
                for (var j = 0; j < series[i].length; j++) {
                    item[j + 1] = series[i][j];
                }
            }

            items.push(item);
        }

        if (!this.chart) {
            this.chart = c3.generate({
                bindto: "#" + this.element.attr("id"),
                data: {
                    columns: items,
                    types: types,
                    groups: [groups]
                },
                axis: {
                    x: {
                        type: "category",
                        categories: axisX,
                        tick: {
                            rotate: -75,
                            multiline: true
                        },
                        height: 100,
                        label: {
                            text: xLabel,
                            position: "outer-right"
                        }
                    },
                    y: {
                        label: {
                            text: yLabel,
                            position: "outer-top"
                        }
                    }
                }
            });
        } else {
            this.chart.groups([
                groups
            ]);
            this.chart.load({
                columns: items
            });
        }


    };

    NRecoPivotChart.prototype.destroy = function () {
        if (this.chart) {
            this.chart.detach();
            this.chart = null;
        }
    };

    $.fn.nrecoPivotChart = function (options) {
        if (typeof options == "string") {
            var instance = this.data("_nrecoPivotChart");
            if (instance) {
                if ((typeof instance[options]) == "function") {
                    return instance[options].apply(instance, Array.prototype.slice.call(arguments, 1));
                } else {
                    $.error("Method " + options + " does not exist");
                }
            } else {
                return; // nothing to do
            }
        }
        return this.each(function () {
            var opts = $.extend({}, $.fn.nrecoPivotChart.defaults, options);
            var $holder = $(this);

            if (!$.data(this, "_nrecoPivotChart")) {
                $.data(this, "_nrecoPivotChart", new NRecoPivotChart($holder, opts));
            }
        });

    };

    $.fn.nrecoPivotChart.defaults = {
        pivotData: {},
        chartOptions: {},
        created: null,
        chartType: "line"
    };

    $.fn.nrecoPivotChart.version = 1.0;

})(jQuery);