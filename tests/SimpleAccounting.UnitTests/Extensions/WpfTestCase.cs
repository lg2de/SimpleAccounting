// https://github.com/xunit/samples.xunit/tree/1173213302fd3a45bd6e4303371d2f78f1743552/STAExamples

namespace lg2de.SimpleAccounting.UnitTests.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Threading;
    using Xunit;
    using Xunit.Abstractions;
    using Xunit.Sdk;

    [DebuggerDisplay(
        @"\{ class = {TestMethod.TestClass.Class.Name}, method = {TestMethod.Method.Name}, display = {DisplayName}, skip = {SkipReason} \}")]
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public class WpfTestCase : LongLivedMarshalByRefObject, IXunitTestCase
    {
        IXunitTestCase testCase;

        public WpfTestCase(IXunitTestCase testCase)
        {
            this.testCase = testCase;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer", true)]
        public WpfTestCase()
        {
        }

        public IMethodInfo Method => this.testCase.Method;

        public string DisplayName => this.testCase.DisplayName;

        public string SkipReason => this.testCase.SkipReason;

        public ITestMethod TestMethod => this.testCase.TestMethod;

        public object[] TestMethodArguments => this.testCase.TestMethodArguments;

        public Dictionary<string, List<string>> Traits => this.testCase.Traits;

        public int Timeout { get; } = 0;

        public Exception InitializationException => null;

        public ISourceInformation SourceInformation
        {
            get => this.testCase.SourceInformation;
            set => this.testCase.SourceInformation = value;
        }

        public string UniqueID => this.testCase.UniqueID;

        public void Deserialize(IXunitSerializationInfo info)
        {
            this.testCase = info.GetValue<IXunitTestCase>("InnerTestCase");
        }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue("InnerTestCase", this.testCase);
        }

        public Task<RunSummary> RunAsync(
            IMessageSink diagnosticMessageSink,
            IMessageBus messageBus,
            object[] constructorArguments,
            ExceptionAggregator aggregator,
            CancellationTokenSource cancellationTokenSource)
        {
            var tcs = new TaskCompletionSource<RunSummary>();
            var thread = new Thread(() =>
            {
                try
                {
                    // Set up the SynchronizationContext so that any awaits
                    // resume on the STA thread as they would in a GUI app.
                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());

                    // Start off the test method.
                    var testCaseTask = this.testCase.RunAsync(diagnosticMessageSink, messageBus, constructorArguments,
                        aggregator, cancellationTokenSource);

                    // Arrange to pump messages to execute any async work associated with the test.
                    var frame = new DispatcherFrame();
                    Task.Run(async delegate
                    {
                        try
                        {
                            await testCaseTask;
                        }
                        finally
                        {
                            // The test case's execution is done. Terminate the message pump.
                            frame.Continue = false;
                        }
                    });
                    Dispatcher.PushFrame(frame);

                    // Report the result back to the Task we returned earlier.
                    CopyTaskResultFrom(tcs, testCaseTask);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return tcs.Task;
        }

        private static void CopyTaskResultFrom<T>(TaskCompletionSource<T> tcs, Task<T> template)
        {
            if (tcs == null)
            {
                throw new ArgumentNullException(nameof(tcs));
            }

            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (!template.IsCompleted)
            {
                throw new ArgumentException("Task must be completed first.", nameof(template));
            }

            if (template.IsFaulted)
            {
                tcs.SetException(template.Exception);
            }
            else if (template.IsCanceled)
            {
                tcs.SetCanceled();
            }
            else
            {
                tcs.SetResult(template.Result);
            }
        }
    }
}