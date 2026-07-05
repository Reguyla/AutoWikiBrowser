using System;
using System.Collections.Generic;

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
            bool resolveRedirects);

        string Preview(
            ApiEdit editor,
            string title,
            string text);

        SaveInfo Save(
            ApiEdit editor,
            string pageText,
            string summary,
            bool minor,
            WatchOptions watch,
            string contentModel);

        void Login(
            ApiEdit editor,
            string username,
            string password);

        void Logout(ApiEdit editor);

        void QueryApi(
            ApiEdit editor,
            string queryParameters);

        string ParseApi(
            ApiEdit editor,
            Dictionary<string, string> queryParameters);

        void RefreshUserInfo(ApiEdit editor);

        void Reset(ApiEdit editor);

        ApiEdit Clone(ApiEdit editor);
    }

    /// <summary>
    /// Production implementation of IAsyncApiEditModernOperations.
    ///
    /// This preserves the existing behavior by forwarding every operation
    /// directly to the supplied synchronous ApiEdit instance.
    /// </summary>
    internal sealed class ApiEditModernOperations
        : IAsyncApiEditModernOperations
    {
        public PageInfo Open(
            ApiEdit editor,
            string title,
            bool resolveRedirects)
        {
            RequireEditor(editor);

            editor.Open(title, resolveRedirects);
            return editor.Page;
        }

        public string Preview(
            ApiEdit editor,
            string title,
            string text)
        {
            RequireEditor(editor);

            return editor.Preview(title, text);
        }

        public SaveInfo Save(
            ApiEdit editor,
            string pageText,
            string summary,
            bool minor,
            WatchOptions watch,
            string contentModel)
        {
            RequireEditor(editor);

            return editor.Save(
                pageText,
                summary,
                minor,
                watch,
                contentModel);
        }

        public void Login(
            ApiEdit editor,
            string username,
            string password)
        {
            RequireEditor(editor);

            editor.Login(username, password);
        }

        public void Logout(ApiEdit editor)
        {
            RequireEditor(editor);

            editor.Logout();
        }

        public void QueryApi(
            ApiEdit editor,
            string queryParameters)
        {
            RequireEditor(editor);

            editor.QueryApi(queryParameters);
        }

        public string ParseApi(
            ApiEdit editor,
            Dictionary<string, string> queryParameters)
        {
            RequireEditor(editor);

            return editor.ParseApi(queryParameters);
        }

        public void RefreshUserInfo(ApiEdit editor)
        {
            RequireEditor(editor);

            editor.RefreshUserInfo();
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

        private static void RequireEditor(ApiEdit editor)
        {
            if (editor == null)
                throw new ArgumentNullException("editor");
        }
    }
}