#if NETCOREAPP
using Examine;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Skybrud.Umbraco.GridData;
using Skybrud.Umbraco.GridData.Factories;
using Skybrud.Umbraco.GridData.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Examine;
using Umbraco.Extensions;

namespace Our.Umbraco.SearchSpellCheck.Indexing
{
    public class SpellCheckValueSetBuilder : BaseValueSetBuilder<IContent>
    {
        private string[] SUPPORTED_FIELDS = new string[]
        {
            global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextBox,
            global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextArea,
            global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TinyMce,
            global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.Grid,
            global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.BlockList
        };

        private IEnumerable<string> _fields { get; set; }
        private readonly UrlSegmentProviderCollection _urlSegmentProviders;
        private readonly PropertyEditorCollection _propertyEditors;
        private ILogger<SpellCheckValueSetBuilder> _logger { get; set; }
        private readonly GridContext _gridContext;
        private readonly IGridFactory _gridFactory;
        private readonly SpellCheckOptions _options;
        private readonly IShortStringHelper _shortStringHelper;

        public SpellCheckValueSetBuilder(GridContext gridContext, IOptions<SpellCheckOptions> options, ILogger<SpellCheckValueSetBuilder> logger, UrlSegmentProviderCollection urlSegmentProviders, IShortStringHelper shortStringHelper, PropertyEditorCollection propertyEditors, IGridFactory gridFactory) : base(propertyEditors, true)
        {
            _gridContext = gridContext;
            _gridFactory = gridFactory;
            _options = options.Value;
            _fields = _options.IndexedFields;
            _logger = logger;
            _urlSegmentProviders = urlSegmentProviders;
            _shortStringHelper = shortStringHelper;
            _propertyEditors = propertyEditors;

            if (_options.EnableLogging)
            {
                _logger.LogInformation("Indexed fields: {0}", string.Join(", ", _fields));
            }
        }

        /// <inheritdoc />
        public override IEnumerable<ValueSet> GetValueSets(params IContent[] content)
        {
            foreach (var c in content)
            {
                var isVariant = c.ContentType.VariesByCulture();
                var urlValue = c.GetUrlSegment(_shortStringHelper, _urlSegmentProviders);
                var properties = c.Properties.ToList();

                if (_options.EnableLogging)
                {
                    _logger.LogInformation("Properties: ", string.Join(", ", properties.Select(x => x.Alias)));
                }

                properties = properties.Where(x => _fields.Contains(x.Alias)).ToList();

                if (_options.EnableLogging)
                {
                    _logger.LogInformation("Properties filtered by IndexedFields: ", string.Join(", ", properties.Select(x => x.Alias)));
                }

                properties = properties.Where(x => SUPPORTED_FIELDS.Contains(x.PropertyType.PropertyEditorAlias)).ToList();

                if (_options.EnableLogging)
                {
                    _logger.LogInformation("Properties filtered by SUPPORTED_FIELDS: ", string.Join(", ", properties.Select(x => x.Alias)));
                }
                
                if (_options.EnableLogging)
                {
                    _logger.LogInformation("Indexing content {0} ({1})", c.PublishName ?? c.Name, c.Id);
                    _logger.LogInformation("Properties to be indexed: {0}", string.Join(", ", properties.Select(x => x.Alias)));
                }

                var indexValues = new Dictionary<string, object>()
                {
                    ["id"] = c.Id,
                    [UmbracoExamineFieldNames.NodeKeyFieldName] = c.Key,
                    [UmbracoExamineFieldNames.NodeNameFieldName] = c.PublishName ?? c.Name,
                    ["urlName"] = urlValue
                };

                if (isVariant)
                {
                    indexValues[UmbracoExamineFieldNames.VariesByCultureFieldName] = new object[] { "y" };

                    foreach (var culture in c.AvailableCultures)
                    {
                        var lowerCulture = culture.ToLowerInvariant();
                        var variantUrl = c.GetUrlSegment(_shortStringHelper, _urlSegmentProviders, culture);
                        indexValues[$"urlName_{lowerCulture}"] = variantUrl;
                        indexValues[$"nodeName_{lowerCulture}"] = c.GetPublishName(lowerCulture);
                        indexValues[$"{Constants.Internals.FieldName}_{lowerCulture}"] = CollectCleanValues(properties, culture.ToLowerInvariant());
                    }
                }
                else
                {
                    indexValues[Constants.Internals.FieldName] = CollectCleanValues(properties, null);
                }

                if (_options.EnableLogging)
                {
                    _logger.LogInformation("Index values: {0}", JsonConvert.SerializeObject(indexValues));
                }

                var vs = new ValueSet(c.Id.ToInvariantString(), IndexTypes.Content, c.ContentType.Alias, indexValues);

                yield return vs;
            }
        }

#region Private methods
        /// <summary>
        /// Collect clean values from a list of <see cref="Property"/> values
        /// </summary>
        /// <param name="properties">Properties to be checked</param>
        /// <param name="cleanValues">List of clean values to be output</param>
        private string CollectCleanValues(IEnumerable<IProperty> properties, string culture = null)
        {
            List<string> cleanValues = new List<string>();

            foreach (var property in properties)
            {
                var editor = _propertyEditors[property.PropertyType.PropertyEditorAlias];
                var indexVals = editor?.PropertyIndexValueFactory.GetIndexValues(property, culture, null, true);

                if (indexVals != null)
                {
                    foreach (var keyVal in indexVals)
                    {
                        var key = keyVal.Key;

                        if (
                            property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextBox ||
                            property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextArea ||
                            property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TinyMce
                        )
                        {
                            foreach (var val in keyVal.Value)
                            {
                                if (val != null)
                                {
                                    cleanValues.Add(CleanValue(val.ToString()));
                                }
                            }
                        }

                        if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.Grid)
                        {
                            if (key.StartsWith(UmbracoExamineFieldNames.RawFieldPrefix))
                            {
                                foreach (var val in keyVal.Value)
                                {
                                    if (val != null)
                                    {
                                        string json = val.ToString();
                                        GridDataModel gridContent = new GridDataModel(null, null, JObject.Parse(json), _gridFactory);
                                        string searchableText = gridContent.GetSearchableText(_gridContext);
                                        cleanValues.Add(CleanValue(searchableText));
                                    }
                                }
                            }
                        }

                        if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.BlockList)
                        {
                            foreach (var val in keyVal.Value)
                            {
                                if (val != null)
                                {
                                    string json = val.ToString();
                                    GetBlockContent(json, ref cleanValues);
                                }
                            }
                        }
                    }
                }
            }

            return string.Join(" ", cleanValues);
        }

        /// <summary>
        /// Get the internal content of a <see cref="BlockListItem"/>
        /// </summary>
        /// <param name="json"><see cref="string"/> to be parsed</param>
        /// <param name="cleanValues">Reference variable of clean values</param>
        private void GetBlockContent(string json, ref List<string> cleanValues)
        {
            BlockValue blockValue = JsonConvert.DeserializeObject<BlockValue>(json);
            if (blockValue != null)
            {
                foreach (var contentData in blockValue.ContentData)
                {
                    var values = contentData.RawPropertyValues;

                    foreach (var item in values.Where(x => x.Value != null))
                    {
                        try
                        {
                            string itemValue = item.Value.ToString();
                            GetBlockContent(itemValue, ref cleanValues);
                        }
                        catch (Exception ex)
                        {
                            cleanValues.Add(CleanValue(item.Value.ToString()));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Take a <see cref="PropertyValue" /> and strip it down to just its text representation
        /// </summary>
        /// <param name="value">The <see cref="IPropertyValue" /> to be cleaned</param>
        /// <returns>A lowercased clean <see cref="string" /></returns>
        private string CleanValue(IPropertyValue value, bool lowercase = true)
        {
            string result = value.PublishedValue.ToString();
            result = CleanValue(result, lowercase);
            return result;
        }

        /// <summary>
        /// Take a <see cref="string" /> value and strip it down to just its text representation
        /// </summary>
        /// <param name="value">The <see cref="string" /> to be cleaned</param>
        /// <returns>A lowercased clean <see cref="string" /></returns>
        private string CleanValue(string value, bool lowercase = true)
        {
            string result = value;

            // Strip anything that's HTML
            result = HttpUtility.HtmlDecode(result.StripHtml());

            // Replace newlines
            result = result.Replace("\r", " ").Replace("\n", " ");

            // Replace punctuation (except single quotes in the middle of word, e.g. we're, don't)
            result = Regex.Replace(result, @"[^\w' ]+|'(?!\w)|(?<!\w)'", " ");

            // Lowercase all results
            if (lowercase)
            {
                result = result.ToLowerInvariant();
            }

            return result;
        }
#endregion
    }
}
#endif