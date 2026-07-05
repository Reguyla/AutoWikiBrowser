using System;
using System.Collections.Generic;
using System.Threading;
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

        /// <summary>
        /// Creates an AsyncApiEditModern instance whose operations are handled
        /// entirely by the supplied fake. The ApiEdit instance is required by
        /// the production class, but this test never lets the fake call it.
        /// </summary>
        private static AsyncApiEditModern CreateEditor(
            IAsyncApiEditModernOperations operations)
        {
            return new AsyncApiEditModern(
                new ApiEdit("https://example.invalid/w/"),
                null,
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

            // When true, Preview(...) pauses until the test releases it.
            public bool BlockPreview { get; set; }

            // The fake sets this signal after Preview(...) begins executing.
            public ManualResetEventSlim PreviewStarted
            {
                get { return PreviewStartedSignal; }
            }

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

            public PageInfo Open(
                ApiEdit editor,
                string title,
                bool resolveRedirects)
            {
                throw new NotSupportedException(
                    "Open was not configured for this test.");
            }

            public string Preview(
                ApiEdit editor,
                string title,
                string text)
            {
                PreviewCallCount++;
                PreviewEditor = editor;
                PreviewTitle = title;
                PreviewText = text;

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

                return PreviewResult;
            }

            public SaveInfo Save(
                ApiEdit editor,
                string pageText,
                string summary,
                bool minor,
                WatchOptions watch,
                string contentModel)
            {
                throw new NotSupportedException(
                    "Save was not configured for this test.");
            }

            public void Login(
                ApiEdit editor,
                string username,
                string password)
            {
                throw new NotSupportedException(
                    "Login was not configured for this test.");
            }

            public void Logout(ApiEdit editor)
            {
                throw new NotSupportedException(
                    "Logout was not configured for this test.");
            }

            public void QueryApi(
                ApiEdit editor,
                string queryParameters)
            {
                throw new NotSupportedException(
                    "QueryApi was not configured for this test.");
            }

            public string ParseApi(
                ApiEdit editor,
                Dictionary<string, string> queryParameters)
            {
                throw new NotSupportedException(
                    "ParseApi was not configured for this test.");
            }

            public void RefreshUserInfo(ApiEdit editor)
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
    }
}