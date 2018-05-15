# NReco PivotData: embedded BI functionality for web apps
PivotData refers to the following products that share common js front-end widgets:

* [PivotData Toolkit for .NET](https://www.nrecosite.com/pivot_data_library_net.aspx) which provides .NET components for data aggregation, OLAP operations, HTML pivot tables and pivot charts rendering, exports to CSV/JSON/Excel/PDF, data source connectors (SQL/CSV/JSON/MongoDb/ElasticSearch/SSAS MDX). Includes [web pivot table builder](http://pivottable.nrecosite.com/) that can be easily integrated into any ASP.NET application.
* [PivotData microservice](https://www.nrecosite.com/pivotdata_service.aspx) self-hosted web API and ROLAP engine for live web reports generation (pivot tables, charts, datagrids), integrates with any web app.

## Alternative js charts for web pivot
Default implementation of *jquery.nrecoPivotChart.js* from [web pivot table builder](https://www.nrecosite.com/pivotdata/web-pivot-builder.aspx) uses [ChartistJS](https://github.com/gionkunz/chartist-js) that is free lighweight charting library with SVG rendering, but in some cases you might want to use more powerful interactive charts. The following alternative integrations are available:

* [ECharts](https://github.com/nreco/pivotdata/tree/master/webpivot/ECharts)
* [C3.js](https://github.com/nreco/pivotdata/tree/master/webpivot/C3js)
