#if NETCOREAPP
using Umbraco.Extensions;
using Umbraco.Cms.Core.Models;
using Skybrud.Umbraco.GridData;
using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Models.Blocks;
using Skybrud.Umbraco.GridData.Models;
using Umbraco.Cms.Infrastructure.Examine;
using Microsoft.Extensions.Options;
#else
using Umbraco.Core;
using Umbraco.Examine;
using Umbraco.Core.Models;
using Umbraco.Core.Logging;
using System.Configuration;
using Skybrud.Umbraco.GridData;
using Umbraco.Core.Models.Blocks;
using static Umbraco.Core.Models.Property;
#endif
using System;
using Examine;
using System.Web;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Our.Umbraco.SearchSpellCheck.Indexing
{
    public class SpellCheckValueSetBuilder : IValueSetBuilder<IContent>
    {
        private ILogger _logger { get; set; }
        private IEnumerable<string> _fields { get; set; }
#if NETCOREAPP
        private GridContext _gridContext { get; set; }
        private readonly SpellCheckOptions _options;
#endif

#if NETCOREAPP
        public SpellCheckValueSetBuilder(GridContext gridContext, IOptions<SpellCheckOptions> options)
        {
            _gridContext = gridContext;
            _options = options.Value;
            _fields = _options.IndexedFields;
        }
#else
        public SpellCheckValueSetBuilder(ILogger logger)
        {
            _logger = logger;

            var fields = ConfigurationManager.AppSettings[Constants.Configuration.IndexedFields];
            if (!string.IsNullOrEmpty(fields))
            {
                _fields = fields.Split(',').Select(x => x.Trim());
            }
            else
            {
                _fields = new List<string>(new string[] { "nodeName" });
            }
        }
#endif

        /// <inheritdoc />
        public IEnumerable<ValueSet> GetValueSets(params IContent[] content)
        {
            foreach (var c in content)
            {
                List<string> cleanValues = new List<string>();

                var properties = c.Properties.Where(x => _fields.Contains(x.Alias));
                CollectCleanValues(properties, cleanValues);

                var allWords = string.Join(" ", cleanValues);

                var indexValues = new Dictionary<string, object>()
                {
                    ["nodeName"] = c.PublishName,
                    [Constants.Internals.FieldName] = allWords
                };

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
#if NETCOREAPP
        private void CollectCleanValues(IEnumerable<IProperty> properties, List<string> cleanValues)
        {
            foreach (var property in properties)
            {
                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextBox || property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TextArea || property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.TinyMce)
                {
                    foreach (var value in property.Values.WhereNotNull().Where(x => x.PublishedValue != null))
                    {
                        cleanValues.Add(CleanValue(value));
                    }
                }

                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.Grid)
                {
                    foreach (var value in property.Values.WhereNotNull().Where(x => x.PublishedValue != null))
                    {
                        string json = value.PublishedValue.ToString();
                        GridDataModel gridContent = JsonConvert.DeserializeObject<GridDataModel>(json);
                        cleanValues.Add(CleanValue(gridContent.GetSearchableText(_gridContext)));
                    }
                }

                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Cms.Core.Constants.PropertyEditors.Aliases.BlockList)
                {
                    foreach (var value in property.Values.WhereNotNull().Where(x => x.PublishedValue != null))
                    {
                        string json = value.PublishedValue.ToString();
                        GetBlockContent(json, ref cleanValues);
                    }
                }
            }
        }
#else
        private void CollectCleanValues(IEnumerable<Property> properties, List<string> cleanValues)
        {
            foreach (var property in properties)
            {
                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.TextBox || property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.TextArea || property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.TinyMce)
                {
                    foreach (var value in property.Values.WhereNotNull().Where(x => x.PublishedValue != null))
                    {
                        cleanValues.Add(CleanValue(value));
                    }
                }

                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.Grid)
                {
                    foreach (var value in property.Values.WhereNotNull().Where(x => x.PublishedValue != null))
                    {
                        string json = value.PublishedValue.ToString();
                        GridDataModel gridContent = GridDataModel.Deserialize(json);
                        cleanValues.Add(CleanValue(gridContent.GetSearchableText()));
                    }
                }

                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.BlockList)
                {
                    foreach (var value in property.Values.WhereNotNull().Where(x => x.PublishedValue != null))
                    {
                        string json = value.PublishedValue.ToString();
                        GetBlockContent(json, ref cleanValues);
                    }
                }
            }
        }
#endif

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
#if NETCOREAPP
        /// <param name="value">The <see cref="IPropertyValue" /> to be cleaned</param>
#else
        /// <param name="value">The <see cref="PropertyValue" /> to be cleaned</param>
#endif
        /// <returns>A lowercased clean <see cref="string" /></returns>
#if NETCOREAPP
        private string CleanValue(IPropertyValue value, bool lowercase = true)
        {
            string result = value.PublishedValue.ToString();
            result = CleanValue(result, lowercase);
            return result;
        }
#else
        private string CleanValue(PropertyValue value, bool lowercase = true)
        {
            string result = value.PublishedValue.ToString();
            result = CleanValue(result, lowercase);
            return result;
        }
#endif

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