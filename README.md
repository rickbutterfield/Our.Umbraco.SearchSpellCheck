# Our.Umbraco.SearchSpellCheck
A Lucene.Net-based spell checker for Umbraco v8 and v9.

[![NuGet release](https://img.shields.io/nuget/v/Our.Umbraco.SearchSpellCheck.svg)](https://www.nuget.org/packages/Our.Umbraco.SearchSpellCheck/)
[![Build Status](https://dev.azure.com/rickbutterfield/NuGet%20Packages/_apis/build/status/rickbutterfield.Our.Umbraco.SearchSpellCheck?branchName=main)](https://dev.azure.com/rickbutterfield/NuGet%20Packages/_build/latest?definitionId=2&branchName=main)

This project wouldn't exist without [Lars-Erik Aabech](https://github.com/lars-erik) who [created a v7 version of this](https://blog.aabech.no/archive/building-a-spell-checker-for-search-in-umbraco/), which a lot of the work is based on.

## How it works
![alt text](docs/img/screenshot.png?raw=true "A search result, with a misspelt version of the word 'house'. It is being suggested to the user to instead search for the correct spelling of the word.")

On startup, this extension will index all the content in your site based on the `IndexedFields` settings. On every search, the extension will check the multi-word search term against the index and suggest the most likely words to the user.

## Installation
At present, the only way to install this is to use NuGet. You can find the package on [NuGet.org](https://www.nuget.org/packages/Our.Umbraco.SearchSpellCheck/) and install it using the Package Manager UI in Visual Studio.

## Configuration
### v8
When the package is installed, new keys will be added to the `appSettings` section of your `web.config`:
```xml
<add key="Our.Umbraco.SearchSpellCheck.IndexName" value="SpellCheckIndex" />
<add key="Our.Umbraco.SearchSpellCheck.IndexedFields" value="nodeName" />
<add key="Our.Umbraco.SearchSpellCheck.AutoRebuildIndex" value="true" />
<add key="Our.Umbraco.SearchSpellCheck.AutoRebuildDelay" value="5" />
<add key="Our.Umbraco.SearchSpellCheck.AutoRebuildRepeat" value="30" />
```

### v9
In v9 you'll need to use the `appSettings.json` file instead of the `web.config` file.
```
{
    "SearchSpellCheck": {
        "IndexName": "SpellCheckIndex",
        "IndexedFields": [ "nodeName" ],
        "AutoRebuildIndex": true,
        "AutoRebuildDelay": 5,
        "AutoRebuildRepeat": 30
    }
}
```

### `IndexName`
The name of the Lucene index to be created. This is the also name of the folder in the `App_Data` folder that contains the Lucene index. By default it is `SpellCheckIndex` but this can be changed if you need a different naming convention.

### `IndexedFields`
The alias(es) of fields to be indexed. This is a comma-separated list of field names. By default only the `nodeName` field is indexed. Currently, there is support for text, [Grid Layout](https://our.umbraco.com/Documentation/Fundamentals/Backoffice/property-editors/built-in-property-editors/Grid-Layout/) and [Block List Editor](https://our.umbraco.com/Documentation/Fundamentals/Backoffice/property-editors/built-in-property-editors/Block-List-Editor/) fields.

### `AutoRebuildIndex`
Boolean indicating if you want a background process to run to rebuild the index. Defaults to true.

### `AutoRebuildDelay`
Number of minutes you want to delay the background process from starting. Defaults to 5 minutes.

### `AutoRebuildRepeat`
Number of minutes you want the scheduled background process to run. Defaults to 30 minutes.

## Usage
The package is a single class called `Our.Umbraco.SearchSpellCheck.Suggestions`. This class contains two methods:
- `IOrderedEnumerable<Suggestion> GetSuggestions(string searchTerm)` - returns a list of suggestions for the given word, and their weight.
- `string GetSuggestion(string searchTerm)` - returns the first suggestion for the given word.

### Example
Within a `SearchService`, you could use the `Suggestions` class to get suggestions for a given word:
```csharp
public string GetSuggestion(string searchTerm)
{
    return Suggestions.GetSuggestion(searchTerm);
}
```

This could then be returned within a ViewModel:
```csharp
if (!string.IsNullOrEmpty(viewModel.SearchTerm))
{
    viewModel.SpellCheck = _searchService.GetSuggestion(viewModel.SearchTerm);
}
```

And then in your view:
```csharp
@if (Model.TotalResults == 0 && !string.IsNullOrEmpty(Model.SpellCheck))
{
    @:Did you mean <em><a href="?q=@Model.SpellCheck">@Model.SpellCheck</a></em>?
}
```

## License
Copyright &copy; 2021 [Rick Butterfield](https://rickbutterfield.com), and other contributors

Licensed under the [MIT License](LICENSE.md).
