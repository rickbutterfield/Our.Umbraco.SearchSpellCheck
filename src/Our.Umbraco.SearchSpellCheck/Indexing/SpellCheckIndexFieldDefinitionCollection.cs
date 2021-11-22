#if NETCOREAPP
using Examine;

namespace Our.Umbraco.SearchSpellCheck
{
    public class SpellCheckIndexFieldDefinitionCollection : FieldDefinitionCollection
    {
        public SpellCheckIndexFieldDefinitionCollection()
            : base(SpellCheckIndexFieldDefinitions)
        {
        }

        public static readonly FieldDefinition[] SpellCheckIndexFieldDefinitions =
        {
            new FieldDefinition(Constants.Internals.FieldName, FieldDefinitionTypes.FullText)
        };
    }
}
#endif