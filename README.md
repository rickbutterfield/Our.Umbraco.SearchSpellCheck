# Our.Umbraco.SearchSpellCheck
A Lucene.Net-based spell checker for Umbraco v8.

[![NuGet release](https://img.shields.io/nuget/v/Our.Umbraco.SearchSpellCheck.svg)](https://www.nuget.org/packages/Our.Umbraco.SearchSpellCheck/)
[![Build Status](https://dev.azure.com/rickbutterfield/NuGet%20Packages/_apis/build/status/rickbutterfield.Our.Umbraco.SearchSpellCheck?branchName=main)](https://dev.azure.com/rickbutterfield/NuGet%20Packages/_build/latest?definitionId=2&branchName=main)

This project wouldn't exist without Lars-Erik Aabech who [created a v7 version of this](https://blog.aabech.no/archive/building-a-spell-checker-for-search-in-umbraco/), which a lot of the work is based on.

## Installation
At present, the only way to install this is to use NuGet. You can find the package on [NuGet.org](https://www.nuget.org/packages/Our.Umbraco.SearchSpellCheck/) and install it using the Package Manager UI in Visual Studio.

## Configuration
When the package is installed, two new keys will be added to the `appSettings` section of your `web.config`:
```xml
<add key="Our.Umbraco.SearchSpellCheck.IndexName" value="SpellCheckIndex" />
<add key="Our.Umbraco.SearchSpellCheck.IndexedFields" value="nodeName" />
```

### `Our.Umbraco.SearchSpellCheck.IndexName`
The name of the Lucene index to be created. This is the also name of the folder in the `App_Data` folder that contains the Lucene index. By default it is `SpellCheckIndex` but this can be changed if you need a different naming convention.

### `Our.Umbraco.SearchSpellCheck.IndexedFields`
The alias(es) of fields to be indexed. This is a comma-separated list of field names. By default only the `nodeName` field is indexed. Currently, there is support for text, [Grid Layout](https://our.umbraco.com/Documentation/Fundamentals/Backoffice/property-editors/built-in-property-editors/Grid-Layout/) and [Block List Editor](https://our.umbraco.com/Documentation/Fundamentals/Backoffice/property-editors/built-in-property-editors/Block-List-Editor/) fields.

## License
Copyright &copy; 2021 [Rick Butterfield](https://rickbutterfield.com), and other contributors

Licensed under the [MIT License](LICENSE.md).