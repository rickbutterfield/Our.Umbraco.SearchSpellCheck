#if !NETCOREAPP
using Examine;
using Newtonsoft.Json;
using Skybrud.Umbraco.GridData;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Blocks;
using Umbraco.Core.PropertyEditors;
using Umbraco.Examine;

namespace Our.Umbraco.SearchSpellCheck.Indexing
{
    public class SpellCheckValueSetBuilderV8 : BaseValueSetBuilder<IContent>
    {
        private string[] SUPPORTED_FIELDS = new string[]
        {
            global::Umbraco.Core.Constants.PropertyEditors.Aliases.TextBox,
            global::Umbraco.Core.Constants.PropertyEditors.Aliases.TextArea,
            global::Umbraco.Core.Constants.PropertyEditors.Aliases.TinyMce,
            global::Umbraco.Core.Constants.PropertyEditors.Aliases.Grid,
            global::Umbraco.Core.Constants.PropertyEditors.Aliases.BlockList
        };

        private IEnumerable<string> _fields { get; set; }
        private readonly PropertyEditorCollection _propertyEditors;

        public SpellCheckValueSetBuilderV8(PropertyEditorCollection propertyEditors) : base(propertyEditors, true)
        {
            _propertyEditors = propertyEditors;

            var fields = ConfigurationManager.AppSettings[Constants.Configuration.IndexedFields];
            _fields = fields.Split(',').ToList();
        }

        /// <inheritdoc />
        public override IEnumerable<ValueSet> GetValueSets(params IContent[] content)
        {
            foreach (var c in content)
            {
                var isVariant = c.ContentType.VariesByCulture();
                var properties = c.Properties.Where(x => _fields.Contains(x.Alias) && SUPPORTED_FIELDS.Contains(x.PropertyType.PropertyEditorAlias));

                var indexValues = new Dictionary<string, object>()
                {
                    ["id"] = c.Id,
                    [UmbracoExamineIndex.NodeKeyFieldName] = c.Key,
                    ["nodeName"] = c.PublishName ?? c.Name,
                };

                if (isVariant)
                {
                    indexValues["__VariesByCulture"] = new object[] { "y" };

                    foreach (var culture in c.AvailableCultures)
                    {
                        var lowerCulture = culture.ToLowerInvariant();
                        indexValues[$"nodeName_{lowerCulture}"] = c.GetPublishName(lowerCulture);
                        indexValues[$"{Constants.Internals.FieldName}_{lowerCulture}"] = CollectCleanValues(properties, culture.ToLowerInvariant());
                    }
                }
                else
                {
                    indexValues[Constants.Internals.FieldName] = CollectCleanValues(properties, null);
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
        private string CollectCleanValues(IEnumerable<Property> properties, string culture = null)
        {
            List<string> cleanValues = new List<string>();

            foreach (var property in properties)
            {
                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.TextBox || property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.TextArea || property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.TinyMce)
                {
                    var propertyValues = property.Values.WhereNotNull().Where(x => x.PublishedValue != null);

                    if (culture != null)
                    {
                        propertyValues = propertyValues.Where(x => x.Culture == culture);
                    }

                    foreach (var value in propertyValues)
                    {
                        cleanValues.Add(CleanValue(value));
                    }
                }

                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.Grid)
                {
                    var propertyValues = property.Values.WhereNotNull().Where(x => x.PublishedValue != null);

                    if (culture != null)
                    {
                        propertyValues = propertyValues.Where(x => x.Culture == culture);
                    }

                    foreach (var value in propertyValues)
                    {
                        string json = value.PublishedValue.ToString();
                        GridDataModel gridContent = GridDataModel.Deserialize(json);
                        string searchableText = gridContent.GetSearchableText();
                        cleanValues.Add(CleanValue(searchableText));
                    }
                }

                if (property.PropertyType.PropertyEditorAlias == global::Umbraco.Core.Constants.PropertyEditors.Aliases.BlockList)
                {
                    var propertyValues = property.Values.WhereNotNull().Where(x => x.PublishedValue != null);

                    if (culture != null)
                    {
                        propertyValues = propertyValues.Where(x => x.Culture == culture);
                    }

                    foreach (var value in propertyValues)
                    {
                        string json = value.PublishedValue.ToString();
                        GetBlockContent(json, ref cleanValues);
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
        private string CleanValue(Property.PropertyValue value, bool lowercase = true)
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