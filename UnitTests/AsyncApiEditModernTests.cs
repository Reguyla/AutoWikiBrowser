using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using WikiFunctions.API;

namespace UnitTests
{
    /// <summary>
    /// Tests the task-based AsyncApiEditModern wrapper without network access.
    ///
    /// Each test supplies a fake IAsyncApiEditModernOperations implementation.
    /// The fake controls the result returned to AsyncApiEditModern and never
    /// calls the real ApiEdit instance.
    /// </summary>
    [TestFixture]
    [Category("AsyncApiEditModern")]
    public class AsyncApiEditModernTests
    {
        [Test]
        public void PreviewAsync_ReturnsResultFromInjectedOperations()
        {
            FakeOperations operations = new FakeOperations
            {
                PreviewResult = "<p>Controlled preview result</p>"
            };

            AsyncApiEditModern editor = CreateEditor(operations);

            string result = editor.PreviewAsync(
                "Sandbox",
                "Controlled test text").GetAwaiter().GetResult();

            Assert.That(
                result,
                Is.EqualTo("<p>Controlled preview result</p>"));

            Assert.That(operations.PreviewCallCount, Is.EqualTo(1));
            Assert.That(operations.PreviewTitle, Is.EqualTo("Sandbox"));

            Assert.That(
                operations.PreviewText,
                Is.EqualTo("Controlled test text"));

            Assert.That(
                operations.PreviewEditor,
                Is.SameAs(editor.SynchronousEditor));

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Ready));

            Assert.That(editor.IsActive, Is.False);
        }

        [Test]
        public void PreviewAsync_PassesLinkedCancellationTokenToOperationsAdapter()
        {
            FakeOperations operations = new FakeOperations
            {
                BlockPreview = true,
                PreviewResult = "<p>Token propagation preview</p>"
            };

            AsyncApiEditModern editor = CreateEditor(operations);

            CancellationTokenSource cancellation =
                new CancellationTokenSource();

            Task<string> previewTask = editor.PreviewAsync(
                "Sandbox",
                "Text for token propagation",
                cancellation.Token);

            try
            {
                Assert.That(
                    operations.PreviewStarted.Wait(TimeSpan.FromSeconds(5)),
                    Is.True,
                    "The preview operation did not start within the test timeout.");

                Assert.That(operations.PreviewCallCount, Is.EqualTo(1));

                Assert.That(
                    operations.PreviewCancellationToken.CanBeCanceled,
                    Is.True);

                Assert.That(
                    operations.PreviewCancellationToken.IsCancellationRequested,
                    Is.False);

                cancellation.Cancel();

                Assert.That(
                    operations.PreviewCancellationToken.IsCancellationRequested,
                    Is.True);
            }
            finally
            {
                operations.AllowPreviewToComplete.Set();
            }

            Assert.That(
                previewTask.Wait(TimeSpan.FromSeconds(5)),
                Is.True,
                "The preview operation did not complete within the test timeout.");

            string result = previewTask.GetAwaiter().GetResult();

            Assert.That(
                result,
                Is.EqualTo("<p>Token propagation preview</p>"));
        }

        [Test]
        public void ActivePreview_RejectsConcurrentOperationsResetAndClone()
        {
            FakeOperations operations = new FakeOperations
            {
                BlockPreview = true,
                PreviewResult = "<p>Completed preview</p>"
            };

            AsyncApiEditModern editor = CreateEditor(operations);

            var runningPreview = editor.PreviewAsync(
                "Sandbox",
                "Text being previewed");

            try
            {
                // Wait until the fake confirms that the worker operation has
                // started and is holding AsyncApiEditModern's operation gate.
                Assert.That(
                    operations.PreviewStarted.Wait(TimeSpan.FromSeconds(5)),
                    Is.True,
                    "The preview operation did not start within the test timeout.");

                Assert.That(editor.IsActive, Is.True);

                Assert.That(
                    editor.State,
                    Is.EqualTo(AsyncApiEditModern.EditState.Working));

                // A second operation must be rejected while the first one owns
                // the shared-operation gate.
                Assert.That(
                    delegate
                    {
                        editor.PreviewAsync("Other page", "Other text");
                    },
                    Throws.TypeOf<InvocationException>());

                // Reset must not touch the shared ApiEdit while preview is active.
                Assert.That(
                    delegate
                    {
                        editor.Reset();
                    },
                    Throws.TypeOf<InvocationException>());

                // Clone must not clone the shared ApiEdit while preview is active.
                Assert.That(
                    delegate
                    {
                        editor.Clone();
                    },
                    Throws.TypeOf<InvocationException>());
            }
            finally
            {
                // Always release the blocked preview, including when an assertion
                // above fails. This prevents a worker task being left running.
                operations.AllowPreviewToComplete.Set();
            }

            // The fake should now be allowed to return normally.
            Assert.That(
                runningPreview.Wait(TimeSpan.FromSeconds(5)),
                Is.True,
                "The preview operation did not complete within the test timeout.");

            string result = runningPreview.GetAwaiter().GetResult();

            Assert.That(result, Is.EqualTo("<p>Completed preview</p>"));
            Assert.That(editor.IsActive, Is.False);

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Ready));
        }

        [Test]
        public void ActivePreview_WhenCancellationRequested_CompletesWithActualResult()
        {
            FakeOperations operations = new FakeOperations
            {
                BlockPreview = true,
                PreviewResult = "<p>Preview completed after cancellation request</p>"
            };

            AsyncApiEditModern editor = CreateEditor(operations);

            ManualResetEventSlim abortedRaised =
                new ManualResetEventSlim(false);

            editor.Aborted +=
                delegate (object sender, EventArgs e)
                {
                    abortedRaised.Set();
                };

            var runningPreview = editor.PreviewAsync(
                "Sandbox",
                "Text being previewed");

            try
            {
                // Wait until the fake Preview(...) method has started. At this point,
                // the synchronous operation is already in progress and owns the gate.
                Assert.That(
                    operations.PreviewStarted.Wait(TimeSpan.FromSeconds(5)),
                    Is.True,
                    "The preview operation did not start within the test timeout.");

                Assert.That(editor.IsActive, Is.True);

                Assert.That(
                    editor.State,
                    Is.EqualTo(AsyncApiEditModern.EditState.Working));

                // This is a cooperative cancellation request. It cannot interrupt the
                // already-running synchronous fake Preview(...) call.
                Assert.That(editor.CancelCurrentOperation(), Is.True);

                // The operation should remain active until the underlying synchronous
                // call is allowed to return.
                Assert.That(editor.IsActive, Is.True);

                Assert.That(
                    editor.State,
                    Is.EqualTo(AsyncApiEditModern.EditState.Working));
            }
            finally
            {
                // Let the fake synchronous operation finish normally.
                operations.AllowPreviewToComplete.Set();
            }

            Assert.That(
                runningPreview.Wait(TimeSpan.FromSeconds(5)),
                Is.True,
                "The preview operation did not complete within the test timeout.");

            string result = runningPreview.GetAwaiter().GetResult();

            // The actual successful result must be preserved. A late cancellation
            // request must not make this operation appear to have been aborted.
            Assert.That(
                result,
                Is.EqualTo("<p>Preview completed after cancellation request</p>"));

            Assert.That(editor.IsActive, Is.False);

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Ready));

            Assert.That(abortedRaised.IsSet, Is.False);
        }

        [Test]
        public void PreviewAsync_WhenTokenIsAlreadyCanceled_ReturnsCanceledTaskWithoutInvokingPreview()
        {
            FakeOperations operations = new FakeOperations
            {
                PreviewResult = "<p>This result should never be returned</p>"
            };

            AsyncApiEditModern editor = CreateEditor(operations);

            CancellationTokenSource cancellation =
                new CancellationTokenSource();

            cancellation.Cancel();

            Task<string> previewTask = editor.PreviewAsync(
                "Sandbox",
                "Text that should not be previewed",
                cancellation.Token);

            Assert.That(previewTask.IsCanceled, Is.True);

            Assert.That(operations.PreviewCallCount, Is.EqualTo(0));

            Assert.That(editor.IsActive, Is.False);

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Ready));
        }

        [Test]
        public void PreviewAsync_WhenOperationFails_SetsFailedStateAndRaisesOperationFailed()
        {
            InvalidOperationException expectedException =
                new InvalidOperationException("Controlled preview failure.");

            FakeOperations operations = new FakeOperations
            {
                PreviewExceptionFactory =
                    delegate (ApiEdit apiEdit)
                    {
                        return expectedException;
                    }
            };

            AsyncApiEditModern editor = CreateEditor(operations);

            string reportedOperationName = null;
            Exception reportedException = null;

            ManualResetEventSlim operationFailedRaised =
                new ManualResetEventSlim(false);

            editor.OperationFailed +=
                delegate (object sender, AsyncApiEditOperationFailedEventArgs e)
                {
                    reportedOperationName = e.OperationName;
                    reportedException = e.Exception;
                    operationFailedRaised.Set();
                };

            Task<string> previewTask = editor.PreviewAsync(
                "Sandbox",
                "Text that triggers a normal failure");

            Assert.That(
                delegate
                {
                    previewTask.GetAwaiter().GetResult();
                },
                Throws.TypeOf<InvalidOperationException>());

            Assert.That(
                operationFailedRaised.Wait(TimeSpan.FromSeconds(5)),
                Is.True,
                "OperationFailed was not raised within the test timeout.");

            Assert.That(
                reportedOperationName,
                Is.EqualTo("Preview"));

            Assert.That(
                reportedException,
                Is.SameAs(expectedException));

            Assert.That(operations.PreviewCallCount, Is.EqualTo(1));

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Failed));

            Assert.That(editor.IsActive, Is.False);
        }

        [Test]
        public void PreviewAsync_WhenLoggedOff_RaisesLoggedOffWithoutOperationFailed()
        {
            FakeOperations operations = new FakeOperations
            {
                PreviewExceptionFactory =
                    delegate (ApiEdit apiEdit)
                    {
                        return new LoggedOffException(apiEdit);
                    }
            };

            AsyncApiEditModern editor = CreateEditor(operations);

            ManualResetEventSlim loggedOffRaised =
                new ManualResetEventSlim(false);

            ManualResetEventSlim operationFailedRaised =
                new ManualResetEventSlim(false);

            editor.LoggedOff +=
                delegate (object sender, EventArgs e)
                {
                    loggedOffRaised.Set();
                };

            editor.OperationFailed +=
                delegate (object sender, AsyncApiEditOperationFailedEventArgs e)
                {
                    operationFailedRaised.Set();
                };

            var previewTask = editor.PreviewAsync(
                "Sandbox",
                "Text that triggers a logged-off failure");

            Assert.That(
                delegate
                {
                    previewTask.GetAwaiter().GetResult();
                },
                Throws.TypeOf<LoggedOffException>());

            Assert.That(
                loggedOffRaised.Wait(TimeSpan.FromSeconds(5)),
                Is.True,
                "LoggedOff was not raised within the test timeout.");

            // Once LoggedOff has been raised, ReportFailure(...) has completed its
            // special-case branch. This failure must not also be reported as a
            // generic OperationFailed event.
            Assert.That(operationFailedRaised.IsSet, Is.False);

            Assert.That(operations.PreviewCallCount, Is.EqualTo(1));

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Failed));

            Assert.That(editor.IsActive, Is.False);
        }

        [Test]
        public void PreviewAsync_WhenMaxlagOccurs_RaisesMaxlagExceededWithoutOperationFailed()
        {
            const double expectedMaxlag = 12.5;
            const int expectedRetryAfter = 10;

            FakeOperations operations = new FakeOperations
            {
                PreviewExceptionFactory =
                    delegate (ApiEdit apiEdit)
                    {
                        return new MaxlagException(
                            apiEdit,
                            expectedMaxlag,
                            expectedRetryAfter);
                    }
            };

            AsyncApiEditModern editor = CreateEditor(operations);

            ManualResetEventSlim maxlagRaised =
                new ManualResetEventSlim(false);

            ManualResetEventSlim operationFailedRaised =
                new ManualResetEventSlim(false);

            double reportedMaxlag = 0;
            int reportedRetryAfter = 0;

            editor.MaxlagExceeded +=
                delegate (object sender, AsyncApiEditMaxlagEventArgs e)
                {
                    reportedMaxlag = e.Maxlag;
                    reportedRetryAfter = e.RetryAfter;
                    maxlagRaised.Set();
                };

            editor.OperationFailed +=
                delegate (object sender, AsyncApiEditOperationFailedEventArgs e)
                {
                    operationFailedRaised.Set();
                };

            var previewTask = editor.PreviewAsync(
                "Sandbox",
                "Text that triggers maxlag");

            Assert.That(
                delegate
                {
                    previewTask.GetAwaiter().GetResult();
                },
                Throws.TypeOf<MaxlagException>());

            Assert.That(
                maxlagRaised.Wait(TimeSpan.FromSeconds(5)),
                Is.True,
                "MaxlagExceeded was not raised within the test timeout.");

            // Maxlag is a special API condition. It should not also be raised as
            // the generic OperationFailed compatibility event.
            Assert.That(operationFailedRaised.IsSet, Is.False);

            Assert.That(reportedMaxlag, Is.EqualTo(expectedMaxlag));
            Assert.That(reportedRetryAfter, Is.EqualTo(expectedRetryAfter));

            Assert.That(operations.PreviewCallCount, Is.EqualTo(1));

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Failed));

            Assert.That(editor.IsActive, Is.False);
        }

        [Test]
        public void PreviewAsync_WhenCallbackContextIsSupplied_PostsStateChangedToThatContext()
        {
            FakeOperations operations = new FakeOperations
            {
                PreviewResult = "<p>Context test preview</p>"
            };

            RecordingSynchronizationContext callbackContext =
                new RecordingSynchronizationContext();

            AsyncApiEditModern editor = CreateEditor(
                operations,
                callbackContext);

            int stateChangedCount = 0;
            List<int> eventThreadIds = new List<int>();

            editor.StateChanged +=
                delegate (object sender, EventArgs e)
                {
                    stateChangedCount++;
                    eventThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
                };

            Task<string> previewTask = editor.PreviewAsync(
                "Sandbox",
                "Text for callback-context testing");

            Assert.That(
                previewTask.Wait(TimeSpan.FromSeconds(5)),
                Is.True,
                "The preview operation did not complete within the test timeout.");

            Assert.That(
                callbackContext.WaitForPostCount(
                    2,
                    TimeSpan.FromSeconds(5)),
                Is.True,
                "StateChanged notifications were not posted to the callback context.");

            // The custom context has queued the callbacks but has not executed them.
            // This proves the events were posted rather than invoked directly.
            Assert.That(stateChangedCount, Is.EqualTo(0));

            // Simulate the UI message loop processing queued callbacks.
            callbackContext.RunAll();

            // A successful operation changes state twice:
            // Ready -> Working, then Working -> Ready.
            Assert.That(stateChangedCount, Is.EqualTo(2));

            Assert.That(
                eventThreadIds,
                Is.All.EqualTo(Thread.CurrentThread.ManagedThreadId));

            Assert.That(
                previewTask.GetAwaiter().GetResult(),
                Is.EqualTo("<p>Context test preview</p>"));

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Ready));

            Assert.That(editor.IsActive, Is.False);
        }

        [Test]
        public void PreviewAsync_WhenOperationFails_PostsOperationFailedToCallbackContext()
        {
            InvalidOperationException expectedException =
                new InvalidOperationException("Controlled callback-context failure.");

            FakeOperations operations = new FakeOperations
            {
                PreviewExceptionFactory =
                    delegate (ApiEdit apiEdit)
                    {
                        return expectedException;
                    }
            };

            RecordingSynchronizationContext callbackContext =
                new RecordingSynchronizationContext();

            AsyncApiEditModern editor = CreateEditor(
                operations,
                callbackContext);

            int operationFailedCount = 0;
            string reportedOperationName = null;
            Exception reportedException = null;
            List<int> eventThreadIds = new List<int>();

            editor.OperationFailed +=
                delegate (object sender, AsyncApiEditOperationFailedEventArgs e)
                {
                    operationFailedCount++;
                    reportedOperationName = e.OperationName;
                    reportedException = e.Exception;
                    eventThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
                };

            Task<string> previewTask = editor.PreviewAsync(
                "Sandbox",
                "Text that triggers a callback-context failure");

            // The failure path posts three callbacks:
            // 1. StateChanged for Ready -> Working
            // 2. StateChanged for Working -> Failed
            // 3. OperationFailed for the original exception.
            //
            // Waiting for all three posts also confirms the operation has reached its
            // completion and notification path without calling Task.Wait(...), which
            // would throw AggregateException for an intentionally faulted task.
            Assert.That(
                callbackContext.WaitForPostCount(
                    3,
                    TimeSpan.FromSeconds(5)),
                Is.True,
                "Expected callbacks were not posted to the supplied context.");

            Assert.That(previewTask.IsCompleted, Is.True);

            Assert.That(
                delegate
                {
                    previewTask.GetAwaiter().GetResult();
                },
                Throws.TypeOf<InvalidOperationException>());

            // The callback context queues notifications, so the event handler must
            // not have executed until the test explicitly processes the queue.
            Assert.That(operationFailedCount, Is.EqualTo(0));

            callbackContext.RunAll();

            Assert.That(operationFailedCount, Is.EqualTo(1));

            Assert.That(
                reportedOperationName,
                Is.EqualTo("Preview"));

            Assert.That(
                reportedException,
                Is.SameAs(expectedException));

            Assert.That(
                eventThreadIds,
                Is.All.EqualTo(Thread.CurrentThread.ManagedThreadId));

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Failed));

            Assert.That(editor.IsActive, Is.False);
        }

        [Test]
        public void PreviewAsync_WhenOperationObservesCancellation_CompletesAsAborted()
        {
            FakeOperations operations = new FakeOperations
            {
                BlockPreview = true,
                ThrowWhenCancellationRequested = true,
                PreviewResult = "<p>This result should not be returned</p>"
            };

            AsyncApiEditModern editor = CreateEditor(operations);

            ManualResetEventSlim abortedRaised =
                new ManualResetEventSlim(false);

            editor.Aborted +=
                delegate (object sender, EventArgs e)
                {
                    abortedRaised.Set();
                };

            Task<string> previewTask = editor.PreviewAsync(
                "Sandbox",
                "Text being previewed");

            Assert.That(
                operations.PreviewStarted.Wait(TimeSpan.FromSeconds(5)),
                Is.True,
                "The preview operation did not start within the test timeout.");

            Assert.That(editor.IsActive, Is.True);

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Working));

            Assert.That(editor.CancelCurrentOperation(), Is.True);

            operations.AllowPreviewToComplete.Set();

            Assert.That(
                delegate
                {
                    previewTask.GetAwaiter().GetResult();
                },
                Throws.InstanceOf<OperationCanceledException>());

            Assert.That(
                abortedRaised.Wait(TimeSpan.FromSeconds(5)),
                Is.True,
                "Aborted was not raised within the test timeout.");

            Assert.That(editor.IsActive, Is.False);

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Aborted));
        }

        [Test]
        public void WatchAsync_CallsInjectedOperationsAdapter()
        {
            FakeOperations operations = new FakeOperations();

            AsyncApiEditModern editor = CreateEditor(operations);

            editor.WatchAsync("Sandbox").GetAwaiter().GetResult();

            Assert.That(operations.WatchCallCount, Is.EqualTo(1));
            Assert.That(operations.WatchTitle, Is.EqualTo("Sandbox"));

            Assert.That(
                operations.WatchEditor,
                Is.SameAs(editor.SynchronousEditor));

            Assert.That(
                operations.WatchCancellationToken.CanBeCanceled,
                Is.True);

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Ready));

            Assert.That(editor.IsActive, Is.False);
        }

        [Test]
        public void UnwatchAsync_CallsInjectedOperationsAdapter()
        {
            FakeOperations operations = new FakeOperations();

            AsyncApiEditModern editor = CreateEditor(operations);

            editor.UnwatchAsync("Sandbox").GetAwaiter().GetResult();

            Assert.That(operations.UnwatchCallCount, Is.EqualTo(1));
            Assert.That(operations.UnwatchTitle, Is.EqualTo("Sandbox"));

            Assert.That(
                operations.UnwatchEditor,
                Is.SameAs(editor.SynchronousEditor));

            Assert.That(
                operations.UnwatchCancellationToken.CanBeCanceled,
                Is.True);

            Assert.That(
                editor.State,
                Is.EqualTo(AsyncApiEditModern.EditState.Ready));

            Assert.That(editor.IsActive, Is.False);
        }

        /// <summary>
        /// Creates an AsyncApiEditModern instance whose operations are handled
        /// entirely by the supplied fake. The ApiEdit instance is required by
        /// the production class, but this test never lets the fake call it.
        /// </summary>
        private static AsyncApiEditModern CreateEditor(
            IAsyncApiEditModernOperations operations)
        {
            return CreateEditor(operations, null);
        }

        private static AsyncApiEditModern CreateEditor(
            IAsyncApiEditModernOperations operations,
            SynchronizationContext callbackContext)
        {
            return new AsyncApiEditModern(
                new ApiEdit("https://example.invalid/w/"),
                callbackContext,
                operations);
        }

        /// <summary>
        /// Minimal fake adapter for no-network AsyncApiEditModern tests.
        ///
        /// Individual tests configure only the operation they need. All other
        /// operations throw so a test fails clearly if it calls something
        /// unexpected.
        /// </summary>
        private sealed class FakeOperations
    : IAsyncApiEditModernOperations
        {
            public string PreviewResult { get; set; }

            // Allows an individual test to make Preview(...) throw a controlled
            // exception using the same ApiEdit instance passed into the operation.
            public Func<ApiEdit, Exception> PreviewExceptionFactory { get; set; }

            // When set, Preview(...) throws this exception instead of returning a result.
            public Exception PreviewException { get; set; }

            // When true, Preview(...) pauses until the test releases it.
            public bool BlockPreview { get; set; }

            // The fake sets this signal after Preview(...) begins executing.
            public ManualResetEventSlim PreviewStarted
            {
                get { return PreviewStartedSignal; }
            }

            // When true, Preview(...) checks the supplied cancellation token
            // before returning and throws if cancellation has been requested.
            public bool ThrowWhenCancellationRequested { get; set; }

            // The test sets this signal to allow the blocked preview to finish.
            public ManualResetEventSlim AllowPreviewToComplete
            {
                get { return AllowPreviewToCompleteSignal; }
            }

            private readonly ManualResetEventSlim PreviewStartedSignal =
                new ManualResetEventSlim(false);

            private readonly ManualResetEventSlim AllowPreviewToCompleteSignal =
                new ManualResetEventSlim(false);

            public int PreviewCallCount { get; private set; }

            public ApiEdit PreviewEditor { get; private set; }

            public string PreviewTitle { get; private set; }

            public string PreviewText { get; private set; }

            public int WatchCallCount { get; private set; }

            public ApiEdit WatchEditor { get; private set; }

            public string WatchTitle { get; private set; }

            public CancellationToken WatchCancellationToken { get; private set; }

            public int UnwatchCallCount { get; private set; }

            public ApiEdit UnwatchEditor { get; private set; }

            public string UnwatchTitle { get; private set; }

            public CancellationToken UnwatchCancellationToken { get; private set; }

            // Stored now so the next test can verify that AsyncApiEditModern passes
            // its operation token through the adapter boundary.
            public CancellationToken PreviewCancellationToken { get; private set; }

            public PageInfo Open(
                ApiEdit editor,
                string title,
                bool resolveRedirects,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException(
                    "Open was not configured for this test.");
            }

            public string Preview(
                ApiEdit editor,
                string title,
                string text,
                CancellationToken cancellationToken)
            {
                PreviewCallCount++;
                PreviewEditor = editor;
                PreviewTitle = title;
                PreviewText = text;
                PreviewCancellationToken = cancellationToken;

                if (PreviewExceptionFactory != null)
                {
                    Exception exception = PreviewExceptionFactory(editor);

                    if (exception != null)
                        throw exception;
                }

                if (BlockPreview)
                {
                    // Tell the test the asynchronous worker reached the fake
                    // Preview operation and is now holding the operation gate.
                    PreviewStartedSignal.Set();

                    // Hold the fake operation open until the test performs its
                    // concurrent-operation, Reset, and Clone checks.
                    if (!AllowPreviewToCompleteSignal.Wait(
                        TimeSpan.FromSeconds(10)))
                    {
                        throw new TimeoutException(
                            "The test did not release the blocked preview operation.");
                    }
                }

                if (PreviewException != null)
                    throw PreviewException;

                if (ThrowWhenCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                return PreviewResult;
            }

            public SaveInfo Save(
                ApiEdit editor,
                string pageText,
                string summary,
                bool minor,
                WatchOptions watch,
                string contentModel,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException(
                    "Save was not configured for this test.");
            }

            public void Login(
                ApiEdit editor,
                string username,
                string password,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException(
                    "Login was not configured for this test.");
            }

            public void Logout(
                ApiEdit editor,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException(
                    "Logout was not configured for this test.");
            }

            public void Watch(
                ApiEdit editor,
                string title,
                CancellationToken cancellationToken)
            {
                WatchCallCount++;
                WatchEditor = editor;
                WatchTitle = title;
                WatchCancellationToken = cancellationToken;
            }

            public void Unwatch(
                ApiEdit editor,
                string title,
                CancellationToken cancellationToken)
            {
                UnwatchCallCount++;
                UnwatchEditor = editor;
                UnwatchTitle = title;
                UnwatchCancellationToken = cancellationToken;
            }
            public void QueryApi(
                ApiEdit editor,
                string queryParameters,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException(
                    "QueryApi was not configured for this test.");
            }

            public string ParseApi(
                ApiEdit editor,
                Dictionary<string, string> queryParameters,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException(
                    "ParseApi was not configured for this test.");
            }

            public void RefreshUserInfo(
                ApiEdit editor,
                CancellationToken cancellationToken)
            {
                throw new NotSupportedException(
                    "RefreshUserInfo was not configured for this test.");
            }

            public void Reset(ApiEdit editor)
            {
                throw new NotSupportedException(
                    "Reset was not configured for this test.");
            }

            public ApiEdit Clone(ApiEdit editor)
            {
                throw new NotSupportedException(
                    "Clone was not configured for this test.");
            }
        }

        /// <summary>
        /// Test synchronization context that queues posted callbacks until a test
        /// explicitly runs them. This lets tests verify that AsyncApiEditModern uses
        /// SynchronizationContext.Post rather than invoking event handlers directly.
        /// </summary>
        private sealed class RecordingSynchronizationContext
            : SynchronizationContext
        {
            private readonly object SyncRoot = new object();

            private readonly Queue<PostedCallback> PostedCallbacks =
                new Queue<PostedCallback>();

            private int PostCount;

            public override void Post(
                SendOrPostCallback callback,
                object state)
            {
                if (callback == null)
                    throw new ArgumentNullException("callback");

                lock (SyncRoot)
                {
                    PostedCallbacks.Enqueue(
                        new PostedCallback(callback, state));

                    PostCount++;

                    Monitor.PulseAll(SyncRoot);
                }
            }

            /// <summary>
            /// Waits until at least the requested number of callbacks have been
            /// queued through Post(...).
            /// </summary>
            public bool WaitForPostCount(
                int expectedPostCount,
                TimeSpan timeout)
            {
                DateTime deadline = DateTime.UtcNow.Add(timeout);

                lock (SyncRoot)
                {
                    while (PostCount < expectedPostCount)
                    {
                        TimeSpan remaining = deadline - DateTime.UtcNow;

                        if (remaining <= TimeSpan.Zero)
                            return false;

                        Monitor.Wait(SyncRoot, remaining);
                    }

                    return true;
                }
            }

            /// <summary>
            /// Executes all callbacks that have been posted so far.
            /// </summary>
            public void RunAll()
            {
                while (true)
                {
                    PostedCallback postedCallback;

                    lock (SyncRoot)
                    {
                        if (PostedCallbacks.Count == 0)
                            return;

                        postedCallback = PostedCallbacks.Dequeue();
                    }

                    postedCallback.Callback(postedCallback.State);
                }
            }

            private sealed class PostedCallback
            {
                public PostedCallback(
                    SendOrPostCallback callback,
                    object state)
                {
                    Callback = callback;
                    State = state;
                }

                public SendOrPostCallback Callback { get; private set; }

                public object State { get; private set; }
            }
        }
    }
}