// from https://github.com/xunit/xunit/tree/main/src/common.tests/CultureAwareTesting
// licensed by Apache 2.0 https://github.com/xunit/xunit/blob/main/LICENSE

// ReSharper disable once CheckNamespace
namespace Xunit
{
    using Xunit.Sdk;

    [XunitTestCaseDiscoverer("Xunit." + nameof(CulturedFactAttributeDiscoverer), "SimpleAccounting.UnitTests")]
	internal sealed class CulturedFactAttribute : FactAttribute
	{
		public CulturedFactAttribute(params string[] cultures) { }
	}
}