// https://github.com/xunit/samples.xunit/tree/1173213302fd3a45bd6e4303371d2f78f1743552/STAExamples

namespace lg2de.SimpleAccounting.UnitTests.Extensions
{
    using System.Collections.Generic;
    using System.Linq;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    public class WpfFactDiscoverer : IXunitTestCaseDiscoverer
    {
        readonly FactDiscoverer factDiscoverer;

        public WpfFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            this.factDiscoverer = new FactDiscoverer(diagnosticMessageSink);
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            return this.factDiscoverer
                .Discover(discoveryOptions, testMethod, factAttribute)
                .Select(testCase => new WpfTestCase(testCase));
        }
    }
}