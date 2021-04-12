// from https://github.com/xunit/xunit/tree/main/src/common.tests/CultureAwareTesting
// licensed by Apache 2.0 https://github.com/xunit/xunit/blob/main/LICENSE

#nullable enable
// ReSharper disable once CheckNamespace
namespace Xunit
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class CulturedXunitTheoryTestCaseRunner : XunitTheoryTestCaseRunner
    {
        readonly string culture;
        CultureInfo? originalCulture;
        CultureInfo? originalUICulture;

        public CulturedXunitTheoryTestCaseRunner(
            CulturedXunitTheoryTestCase culturedXunitTheoryTestCase,
            string displayName,
            string? skipReason,
            object?[] constructorArguments,
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
            : base(
                culturedXunitTheoryTestCase, displayName, skipReason, constructorArguments, diagnosticMessageSink,
                messageBus, aggregator, cancellationTokenSource)
        {
            this.culture = culturedXunitTheoryTestCase.Culture;
        }

        protected override Task AfterTestCaseStartingAsync()
        {
            try
            {
                this.originalCulture = CultureInfo.CurrentCulture;
                this.originalUICulture = CultureInfo.CurrentUICulture;

                var cultureInfo = new CultureInfo(this.culture);
                CultureInfo.CurrentCulture = cultureInfo;
                CultureInfo.CurrentUICulture = cultureInfo;
            }
            catch (Exception ex)
            {
                this.Aggregator.Add(ex);
                return Task.FromResult(0);
            }

            return base.AfterTestCaseStartingAsync();
        }

        protected override Task BeforeTestCaseFinishedAsync()
        {
            if (this.originalUICulture != null)
            {
                CultureInfo.CurrentUICulture = this.originalUICulture;
            }

            if (this.originalCulture != null)
            {
                CultureInfo.CurrentCulture = this.originalCulture;
            }

            return base.BeforeTestCaseFinishedAsync();
        }
    }
}
