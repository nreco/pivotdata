# NReco.PivotData [![NuGet Release](https://img.shields.io/nuget/v/NReco.PivotData.svg)](https://www.nuget.org/packages/NReco.PivotData/)
OLAP library that implements:

* in-memory multidimensional dataset (`PivotData` class)
* OLAP operations: roll-up, slice and dice (`SliceQuery` class)
* pivot table data model with efficient totals/sub-totals calculation (`PivotTable` class)

Official component page: [PivotData Toolkit for .NET](https://www.nrecosite.com/pivot_data_library_net.aspx)

# Documentation

* [Getting Started](https://www.nrecosite.com/pivotdata/cube-basics.aspx)
* [Aggregate Functions](https://www.nrecosite.com/pivotdata/aggregate-functions.aspx)
* [Implement custom aggregator](https://www.nrecosite.com/pivotdata/implement-custom-aggregator.aspx)
* [Query/Filter the cube](https://www.nrecosite.com/pivotdata/query-cube.aspx)
* [Sort pivot table data by labels or values](https://www.nrecosite.com/pivotdata/sort-pivot-table.aspx)
* [Create HTML pivot table](https://www.nrecosite.com/pivotdata/create-pivot-table.aspx)
* [Pivot a DataTable](https://www.nrecosite.com/pivotdata/pivot-datatable.aspx)
* [Create Excel PivotTable](https://www.nrecosite.com/pivotdata/create-excel-pivot-table.aspx)
* [API Reference](https://www.nrecosite.com/doc/NReco.PivotData/)

# Examples

* [CsvDemo](https://github.com/nreco/pivotdata/tree/master/examples/NReco.PivotData.Examples.CsvDemo): how to aggregate data from CSV file
* [DynamicFormulaMeasure](https://github.com/nreco/pivotdata/tree/master/examples/NReco.PivotData.Examples.DynamicFormulaMeasure): how to define formula-based measure dynamically (with a user-entered string expression)
* [DynamicListGrouping](https://github.com/nreco/pivotdata/tree/master/examples/NReco.PivotData.Examples.DynamicListGrouping): group objects by multiple fields and calculate aggregates
* [ExcelPivotTable](https://github.com/nreco/pivotdata/tree/master/examples/NReco.PivotData.Examples.ExcelPivotTable): generates Excel PivotTable by PivotData's PivotTable
* [ParallelCube](https://github.com/nreco/pivotdata/tree/master/examples/NReco.PivotData.Examples.ParallelCube): how to perform parallel aggregation (use all CPU cores) and merge all results into one resulting cube
* [PivotTableMvc](https://github.com/nreco/pivotdata/tree/master/examples/NReco.PivotData.Examples.PivotTableMvc): MVC example that renders simple pivot table / charts (without PivotData Toolkit components)
* [QueryCube](https://github.com/nreco/pivotdata/tree/master/examples/NReco.PivotData.Examples.QueryCube): how to make OLAP queries with SliceQuery class: slice, dice, filter, roll-up, calculate derived dimensions and measures

## Who is using this?
NReco.PivotData is in production use at [SeekTable.com](https://www.seektable.com/) and [PivotData microservice](https://www.nrecosite.com/pivotdata_service.aspx). NReco.PivotData is a pre-requisite for PivotData Toolkit components.

## License
Copyright 2015-2025 Vitaliy Fedorchenko

Distributed under the PivotData OLAP library FREE license (see src/LICENSE): NReco.PivotData can be used for free only in non-SaaS apps with one single-server production deployment.
In all other cases commercial license is required (can be purchased [here](https://www.nrecosite.com/pivot_data_library_net.aspx)).
