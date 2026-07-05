using System;
using System.Collections.Generic;
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