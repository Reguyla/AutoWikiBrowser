using System;
using System.Collections.Generic;
using System.Threading;

namespace WikiFunctions.API
{
    /// <summary>
    /// Defines the ApiEdit work performed by AsyncApiEditModern.
    ///
    /// Production code uses ApiEditModernOperations, which forwards to the
    /// real ApiEdit instance. Tests can provide a fake implementation that
    /// returns controlled results or throws controlled exceptions without
    /// contacting a wiki.
    /// </summary>
    internal interface IAsyncApiEditModernOperations
    {
        PageInfo Open(
            ApiEdit editor,
            string title,
            bool resolveRedirects,
            CancellationToken cancellationToken);

        string Preview(
            ApiEdit editor,
            string title,
            string text,
            CancellationToken cancellationToken);

        SaveInfo Save(
            ApiEdit editor,
            string pageText,
            string summary,
            bool minor,
            WatchOptions watch,
            string contentModel,
            CancellationToken cancellationToken);

        void Login(
            ApiEdit editor,
            string username,
            string password,
            CancellationToken cancellationToken);

        void Logout(
            ApiEdit editor,
            CancellationToken cancellationToken);

        void Watch(
            ApiEdit editor,
            string title,
            CancellationToken cancellationToken);

        void Unwatch(
            ApiEdit editor,
            string title,
            CancellationToken cancellationToken);

        void Rollback(
            ApiEdit editor,
            string title,
            string user,
            CancellationToken cancellationToken);

        string HttpGet(
            ApiEdit editor,
            string url,
            CancellationToken cancellationToken);

        void QueryApi(
            ApiEdit editor,
            string queryParameters,
            CancellationToken cancellationToken);

        string ParseApi(
            ApiEdit editor,
            Dictionary<string, string> queryParameters,
            CancellationToken cancellationToken);

        string ExpandTemplates(
            ApiEdit editor,
            string title,
            string text,
            CancellationToken cancellationToken);

        void RefreshUserInfo(
            ApiEdit editor,
            CancellationToken cancellationToken);

        void Reset(ApiEdit editor);

        ApiEdit Clone(ApiEdit editor);
    }

    /// <summary>
    /// Production implementation of IAsyncApiEditModernOperations.
    ///
    /// This preserves the existing behavior by forwarding every operation
    /// directly to the supplied synchronous ApiEdit instance.
    ///
    /// The cancellation token is accepted here so AsyncApiEditModern can pass
    /// one consistently through the operation boundary. Active operations enter
    /// ApiEdit's scoped transport-cancellation path so in-progress HTTP requests
    /// can observe cancellation.
    /// </summary>
    internal sealed class ApiEditModernOperations
        : IAsyncApiEditModernOperations
    {
        public PageInfo Open(
            ApiEdit editor,
            string title,
            bool resolveRedirects,
            CancellationToken cancellationToken)
        {
            return ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    editor.Open(title, resolveRedirects);
                    return editor.Page;
                });
        }

        public string Preview(
            ApiEdit editor,
            string title,
            string text,
            CancellationToken cancellationToken)
        {
            return ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    return editor.Preview(title, text);
                });
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
            return ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    return editor.Save(
                        pageText,
                        summary,
                        minor,
                        watch,
                        contentModel);
                });
        }

        public void Login(
            ApiEdit editor,
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    editor.Login(username, password);
                });
        }

        public void Logout(
            ApiEdit editor,
            CancellationToken cancellationToken)
        {
            ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    editor.Logout();
                });
        }

        public void Watch(
            ApiEdit editor,
            string title,
            CancellationToken cancellationToken)
        {
            ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    editor.Watch(title);
                });
        }

        public void Unwatch(
            ApiEdit editor,
            string title,
            CancellationToken cancellationToken)
        {
            ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    editor.Unwatch(title);
                });
        }

        public void Rollback(
            ApiEdit editor,
            string title,
            string user,
            CancellationToken cancellationToken)
        {
            ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    editor.Rollback(title, user);
                });
        }

        public string HttpGet(
            ApiEdit editor,
            string url,
            CancellationToken cancellationToken)
        {
            return ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    return editor.HttpGet(url);
                });
        }

        public void QueryApi(
            ApiEdit editor,
            string queryParameters,
            CancellationToken cancellationToken)
        {
            ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    editor.QueryApi(queryParameters);
                });
        }

        public string ParseApi(
            ApiEdit editor,
            Dictionary<string, string> queryParameters,
            CancellationToken cancellationToken)
        {
            return ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    return editor.ParseApi(queryParameters);
                });
        }

        public string ExpandTemplates(
            ApiEdit editor,
            string title,
            string text,
            CancellationToken cancellationToken)
        {
            return ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    return editor.ExpandTemplates(title, text);
                });
        }

        public void RefreshUserInfo(
            ApiEdit editor,
            CancellationToken cancellationToken)
        {
            ExecuteWithCancellation(
                editor,
                cancellationToken,
                delegate
                {
                    editor.RefreshUserInfo();
                });
        }

        public void Reset(ApiEdit editor)
        {
            RequireEditor(editor);

            editor.Reset();
        }

        public ApiEdit Clone(ApiEdit editor)
        {
            RequireEditor(editor);

            return (ApiEdit)editor.Clone();
        }

        private static TResult ExecuteWithCancellation<TResult>(
            ApiEdit editor,
            CancellationToken cancellationToken,
            Func<TResult> operation)
        {
            RequireEditor(editor);

            if (operation == null)
                throw new ArgumentNullException("operation");

            using (editor.BeginCancellationScope(cancellationToken))
            {
                return operation();
            }
        }

        private static void ExecuteWithCancellation(
            ApiEdit editor,
            CancellationToken cancellationToken,
            Action operation)
        {
            RequireEditor(editor);

            if (operation == null)
                throw new ArgumentNullException("operation");

            using (editor.BeginCancellationScope(cancellationToken))
            {
                operation();
            }
        }

        private static void RequireEditor(ApiEdit editor)
        {
            if (editor == null)
                throw new ArgumentNullException("editor");
        }
    }
}