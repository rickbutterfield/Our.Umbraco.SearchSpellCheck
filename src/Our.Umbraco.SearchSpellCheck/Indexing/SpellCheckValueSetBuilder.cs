using Examine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Umbraco.Core.Models.Blocks;
using Umbraco.Core.Models;
using Umbraco.Core;
using Umbraco.Core.Logging;
using Umbraco.Examine;
using static Umbraco.Core.Models.Property;
using Skybrud.Umbraco.GridData;
using System.Configuration;

namespace Our.Umbraco.SearchSpellCheck.Indexing
{
    public class SpellCheckValueSetBuilder : IValueSetBuilder<IContent>
    {
        private ILogger _logger { get; set; }
        private IEnumerable<string> _fields { get; set; }

        public SpellCheckValueSetBuilder(ILogger logger)
        {
            _logger = logger;

            var fields = ConfigurationManager.AppSettings[Constants.Configuration.IndexedFields];
            if (!string.IsNullOrEmpty(fields))
            {
                _fields = fields.Split(',').Select(x => x.Trim());
            }
        }

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
                    [Constants.Internals.FieldName] = allWords
                };

                var valueSet = new ValueSet(c.Id.ToString(), Constants.Internals.FieldName, indexValues);

                yield return valueSet;
            }
        }

        #region Private methods
        /// <summary>
        /// Collect clean values from a list of <see cref="Property"/> values
        /// </summary>
        /// <param name="properties">Properties to be checked</param>
        /// <param name="cleanValues">List of clean values to be output</param>
        private void CollectCleanValues(IEnumerable<Property> properties, List<string> cleanValues)
        {
            foreach (var property in properties)
            {
                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.TextBox || property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.TextArea)
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
                            _logger.Error<SpellCheckValueSetBuilder>(ex);
                            cleanValues.Add(CleanValue(item.Value.ToString()));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Take a <see cref="PropertyValue" /> and strip it down to just its text representation
        /// </summary>
        /// <param name="value">The <see cref="PropertyValue" /> to be cleaned</param>
        /// <returns>A lowercased clean <see cref="string" /></returns>
        private string CleanValue(PropertyValue value, bool lowercase = true)
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
