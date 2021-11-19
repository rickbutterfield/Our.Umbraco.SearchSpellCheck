#if !NETCOREAPP
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace Our.Umbraco.SearchSpellCheck.Composing
{
    [RuntimeLevel(MinLevel = RuntimeLevel.Run)]
    public class BuildIndexOnPublishedEventComposer : ComponentComposer<BuildIndexOnPublishedEventComponent>
    { }
}
#endif