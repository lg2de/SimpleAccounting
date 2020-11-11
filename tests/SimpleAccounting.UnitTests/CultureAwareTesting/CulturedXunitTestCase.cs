// from https://github.com/xunit/xunit/tree/main/src/common.tests/CultureAwareTesting
// licensed by Apache 2.0 https://github.com/xunit/xunit/blob/main/LICENSE

#nullable enable
// ReSharper disable once CheckNamespace
namespace Xunit
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    internal class CulturedXunitTestCase : XunitTestCase
	{
		string culture = "<unset>";

		[EditorBrowsable(EditorBrowsableState.Never)]
		[Obsolete("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
		public CulturedXunitTestCase() { }

		public CulturedXunitTestCase(
			IMessageSink diagnosticMessageSink,
			TestMethodDisplay defaultMethodDisplay,
			TestMethodDisplayOptions defaultMethodDisplayOptions,
			ITestMethod testMethod,
			string culture,
			object?[]? testMethodArguments = null)
				: base(diagnosticMessageSink, defaultMethodDisplay, defaultMethodDisplayOptions, testMethod, testMethodArguments)
		{
			Initialize(culture);
		}

		void Initialize(string culture)
		{
			this.culture = culture;

            Traits.Add("Culture", new List<string> { culture });

			DisplayName += $"[{culture}]";
		}

		protected override string GetUniqueID() => $"{base.GetUniqueID()}[{culture}]";

		public override void Deserialize(IXunitSerializationInfo data)
		{
			base.Deserialize(data);

			Initialize(data.GetValue<string>("Culture"));
		}

		public override void Serialize(IXunitSerializationInfo data)
		{
			base.Serialize(data);

			data.AddValue("Culture", culture);
		}

		public override async Task<RunSummary> RunAsync(
			IMessageSink diagnosticMessageSink,
			IMessageBus messageBus,
			object?[] constructorArguments,
			ExceptionAggregator aggregator,
			CancellationTokenSource cancellationTokenSource)
		{
			var originalCulture = CultureInfo.CurrentCulture;
			var originalUICulture = CultureInfo.CurrentUICulture;

			try
			{
				var cultureInfo = new CultureInfo(culture);
				CultureInfo.CurrentCulture = cultureInfo;
				CultureInfo.CurrentUICulture = cultureInfo;

				return await base.RunAsync(diagnosticMessageSink, messageBus, constructorArguments, aggregator, cancellationTokenSource);
			}
			finally
			{
				CultureInfo.CurrentCulture = originalCulture;
				CultureInfo.CurrentUICulture = originalUICulture;
			}
		}
	}
}