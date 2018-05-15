//
// NReco PivotData Pivot Chart Plugin (renders pivot data with ECharts)
// @version 1.0
// @author Vitaliy Fedorchenko
// 
// Copyright (c) Vitaliy Fedorchenko (nrecosite.com) - All Rights Reserved
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY 
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//

(function ($) {

	function NRecoPivotChart(element, options) {
		this.element = element;
		this.options = options;

		init(this);
	}

	function init(pvtChart) {
		var o = pvtChart.options;
		pvtChart.chart = echarts.init(document.getElementById(pvtChart.element.attr('id')));
		if ((typeof pvtChart[o.chartType]) == "function") {
			pvtChart[o.chartType].apply(pvtChart, []);
		} else {
			$.error('Unknown chart type: ' + o.chartType);
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

	function onCreated(pvtChart) {
		if (pvtChart.chart && typeof pvtChart.options.created == "function")
			pvtChart.chart.on('created', pvtChart.options.created);
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

		var chartData = { labels: [], series: [] };
		var addLabels = function (dimKeys, dims) {
			for (var i = 0; i < dimKeys.length; i++)
				chartData.labels.push(dimKeys[i].join(" "));
			if (dims)
				chartData.axisLabel = dims.join(", ");
		};
		if (pivotData.MeasureLabels.length>1) {
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
				chartData.series.push( rowData );
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
				chartData.series.push([ifnull(pivotData.GrandTotal, 0)])
			}
		}
		return chartData;
	};

	function revSeriesData(chartData) {
		for (var i = 0; i < chartData.series.length; i++)
			chartData.series[i].reverse();
		chartData.labels.reverse();
	}

	var createChartInternal = function (pvtChart, seriesType, stacked, horizontal, chartDataCallback, chartOptsCallback) {
		var pivotData = pvtChart.options.pivotData;
		var chartData = pvtChart.getChartData();
		if (chartDataCallback) {
			chartDataCallback(chartData);
		}

		var chartOpts = {
			tooltip: {trigger:'item', transitionDuration:0.0, hideDelay:0},
			xAxis: {
				data: chartData.labels,
				nameLocation: 'center'
			},
			yAxis: {
				nameLocation: 'center'
			},
			series: $.map(chartData.series, function (valArr, idx) {
				var s = {
					data: valArr,
					type: seriesType,
					name: valArr.name
				};
				if (stacked) {
					s.stack = 'a';
				}
				return s;
			})
		};
		chartOpts.yAxis.name = pivotData.MeasureLabels.join(", ");
		chartOpts.xAxis.name = chartData.axisLabel;
		if (horizontal) {
			var tmp = chartOpts.yAxis;
			chartOpts.yAxis = chartOpts.xAxis;
			chartOpts.xAxis = tmp;
		}
		chartOpts.xAxis.nameGap = 25;
		chartOpts.yAxis.nameGap = 45;
		chartOpts = $.extend(chartOpts, pvtChart.options.chartOptions);
		if (chartOptsCallback)
			chartOptsCallback(chartOpts);
		pvtChart.chart.setOption(chartOpts);
		onCreated(pvtChart);
	};

	var getChartWidth = function (data, opts) {
		if (!data) {
			var w = this.element.width();
			if (opts && opts.axisX && opts.axisX.offset)
				w -= opts.axisX.offset;
			if (opts && opts.chartPadding) {
				w -= (opts.chartPadding.left + opts.chartPadding.right);
			}
			return w;
		}
		return data.chartRect.width();
	};
	var getChartHeight = function (data, opts) {
		if (!data) {
			var h = this.element.height();
			if (opts && opts.axisY && opts.axisY.offset)
				h -= opts.axisY.offset;
			if (opts && opts.chartPadding) {
				h -= (opts.chartPadding.top + opts.chartPadding.bottom);
			}
			return h;
		}
		return data.chartRect.height();
	};

	NRecoPivotChart.prototype.bar = function (stacked) {
		createChartInternal(this, 'bar', false, false);
	};

	NRecoPivotChart.prototype.stackedBar = function () {
		createChartInternal(this, 'bar', true, false);
	};

	NRecoPivotChart.prototype.horizontalBar = function (stacked) {
		createChartInternal(this, 'bar', false, true, revSeriesData);
	};

	NRecoPivotChart.prototype.horizontalStackedBar = function () {
		createChartInternal(this, 'bar', true, true, revSeriesData);
	};

	NRecoPivotChart.prototype.pie = function (chartOptsCallback) {
		var pivotData = this.options.pivotData;
		var chartData = this.getChartData(true);
		console.log(chartData);
		var chartOpts = {
			tooltip: { trigger: 'item', transitionDuration: 0.0, hideDelay: 0 },
			series: $.map(chartData.series, function (valArr, idx) {
				return {
					data: $.map(valArr, function (v, vIdx) { return { value: v, name: chartData.labels[vIdx] }; }),
					type: 'pie'
				};
			})
		};
		chartOpts = $.extend(chartOpts, this.options.chartOptions);
		if (chartOptsCallback)
			chartOptsCallback(chartOpts)
		this.chart.setOption(chartOpts);
		onCreated(this);
	};

	NRecoPivotChart.prototype.donut = function () {
		this.pie(function (chartOpts) {
			for (var i = 0; i < chartOpts.series.length; i++) {
				var s = chartOpts.series[i];
				s.radius = ['50%', '70%'];
			}
		});
	};

	NRecoPivotChart.prototype.line = function () {
		createChartInternal(this, 'line', false, false);
	};

	NRecoPivotChart.prototype.scatterplot = function () {
		createChartInternal(this, 'scatter', false, false);
	};

	NRecoPivotChart.prototype.stackedArea = function () {
		createChartInternal(this, 'line', true, false,
			function (chartData) {
				//chartData.series.reverse();
			},
			function (chartOpts) {
				for (var i = 0; i < chartOpts.series.length; i++) {
					var s = chartOpts.series[i];
					s.areaStyle = { normal: {} };
				}
			}
		);
	};

	NRecoPivotChart.prototype.destroy = function () {
		if (this.chart) {
			this.chart.detach();
			this.chart = null;
		}
	};


	$.fn.nrecoPivotChart = function (options) {
		if (typeof options == "string") {
			var instance = this.data('_nrecoPivotChart');
			if (instance) {
				if ((typeof instance[options]) == "function") {
					return instance[options].apply(instance, Array.prototype.slice.call(arguments, 1));
				} else {
					$.error('Method ' + options + ' does not exist');
				}
			} else {
				return; // nothing to do
			}
		}
		return this.each(function () {
			var opts = $.extend({}, $.fn.nrecoPivotChart.defaults, options);
			var $holder = $(this);

			if (!$.data(this, '_nrecoPivotChart')) {
				$.data(this, '_nrecoPivotChart', new NRecoPivotChart($holder, opts));
			}
		});

	};

	$.fn.nrecoPivotChart.defaults = {
		pivotData: {},
		chartOptions: {},
		created: null,
		initAxesLabels : null,
		chartType: "bar"  // line, scatterplot, stackedArea, bar, stackedBar, horizontalBar, horizontalStackedBar, pie, donut
	};

	$.fn.nrecoPivotChart.version = 1.0;

})(jQuery);