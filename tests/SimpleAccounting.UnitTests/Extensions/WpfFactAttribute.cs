// https://github.com/xunit/samples.xunit/tree/1173213302fd3a45bd6e4303371d2f78f1743552/STAExamples

namespace lg2de.SimpleAccounting.UnitTests.Extensions
{
    using System;
    using Xunit;
    using Xunit.Sdk;

    [AttributeUsage(AttributeTargets.Method)]
    [XunitTestCaseDiscoverer(
        "lg2de.SimpleAccounting.UnitTests.Extensions.WpfFactDiscoverer",
        "SimpleAccounting.UnitTests")]
    public class WpfFactAttribute : FactAttribute
    {
    }
}