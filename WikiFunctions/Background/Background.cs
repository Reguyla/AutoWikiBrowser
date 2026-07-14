/*
WikiFunctions
Copyright (C) 2007 Max Semenik

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/
using System.Collections.Specialized;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using WikiFunctions.API;
using WikiFunctions.Lists.Providers;

namespace WikiFunctions.Background
{
    /// <summary>
    /// Represents the method invoked when a background request completes successfully.
    /// </summary>
    /// <param name="req">
    /// The completed <see cref="BackgroundRequest"/> instance containing the
    /// operation result.
    /// </param>
    public delegate void BackgroundRequestComplete(BackgroundRequest req);

    /// <summary>
    /// Represents the method invoked when a background request terminates due to
    /// an unhandled exception.
    /// </summary>
    /// <param name="req">
    /// The <see cref="BackgroundRequest"/> instance containing the exception details.
    /// </param>
    public delegate void BackgroundRequestErrored(BackgroundRequest req);

    /// <summary>
    /// Represents a parameterless method executed on a background worker thread.
    /// </summary>
    public delegate void ExecuteFunctionDelegate();

    /// <summary>
    /// Executes long-running operations on a background thread and provides
    /// completion, error, and cancellation support for the calling code.
    /// </summary>
    public class BackgroundRequest
    {
        /// <summary>
        /// Handles cancellation requested through the progress dialog.
        /// </summary>
        private void PleaseWaitCancelRequested(object sender, EventArgs e)
        {
            Abort();
        }

        /// <summary>
        /// Gets or sets the result produced by the background operation.
        /// </summary>
        public object Result;

        /// <summary>
        /// Gets a value indicating whether the background operation has completed.
        /// If the associated progress dialog is still open, it is closed before
        /// returning.
        /// </summary>
        public bool Done
        {
            get
            {
                bool res = (BgThread != null && (BgThread.ThreadState == ThreadState.Stopped ||
                    BgThread.ThreadState == ThreadState.Aborted));

                try
                {
                    if (res && UI != null) UI.Close();
                }
                catch
                {
                }
                return res;
            }
        }

        /// <summary>
        /// Gets or sets whether a progress dialog should be displayed while the
        /// background operation is running.
        /// </summary>
        public bool HasUI = true;

        /// <summary>
        /// Gets the exception thrown by the background operation, if one occurred.
        /// </summary>
        public Exception ErrorException { get; private set; }

        /// <summary>
        /// Progress dialog associated with the current background operation.
        /// </summary>
        private PleaseWait UI;

        /// <summary>
        /// Worker thread used to execute the current background operation.
        /// </summary>
        private Thread BgThread;

        /// <summary>
        /// Indicates whether cooperative cancellation has been requested for the
        /// current background operation.
        /// </summary>
        private volatile bool _cancellationRequested;

        /// <summary>
        /// Occurs when the background request completes successfully.
        /// </summary>
        public event BackgroundRequestComplete Complete;

        /// <summary>
        /// Occurs when the background request terminates because of an exception.
        /// </summary>
        public event BackgroundRequestErrored Errored;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundRequest"/> class.
        /// </summary>
        public BackgroundRequest()
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundRequest"/> class
        /// and registers a completion handler.
        /// </summary>
        /// <param name="handler">
        /// The method to invoke when the background request completes successfully.
        /// </param>
        public BackgroundRequest(BackgroundRequestComplete handler)
        {
            Complete += handler;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundRequest"/> class
        /// and registers completion and error handlers.
        /// </summary>
        /// <param name="completeHandler">
        /// The method to invoke when the background request completes successfully.
        /// </param>
        /// <param name="errorHandler">
        /// The method to invoke when the background request terminates because of an exception.
        /// </param>
        public BackgroundRequest(
            BackgroundRequestComplete completeHandler,
            BackgroundRequestErrored errorHandler)
            : this(completeHandler)
        {
            Errored += errorHandler;
        }

        /// <summary>
        /// Waits for request to complete
        /// </summary>
        public void Wait()
        {
            while (!Done) Application.DoEvents();
        }

        /// <summary>
        /// Returns ThreadState of the thread, or ThreadState.Unstarted if the thread is null
        /// </summary>
        /// <returns>ThreadState of the thread</returns>
        public ThreadState ThreadStatus()
        {
            if (BgThread != null)
                return BgThread.ThreadState;

            return ThreadState.Unstarted;
        }

        /// <summary>
        /// Requests that the background operation stop cooperatively.
        /// </summary>
        /// <remarks>
        /// Thread.Abort is unsupported on .NET 8. This method records the
        /// cancellation request, closes any progress UI, and returns without
        /// forcibly terminating or waiting for the worker thread.
        /// </remarks>
        public void Abort()
        {
            _cancellationRequested = true;

            if (UI != null)
                UI.Close();

            UI = null;
            Result = null;
        }

        protected string StrParam;
        protected object ObjParam1, ObjParam2, ObjParam3;

        /// <summary>
        /// Creates and starts the worker thread used by the background request.
        /// </summary>
        /// <param name="start">
        /// The method to execute on the new background thread.
        /// </param>
        private void InitThread(ThreadStart start)
        {
            _cancellationRequested = false;

            BgThread = new Thread(start)
            {
                IsBackground = true,
                Name = string.Format(
                    "BackgroundRequest (StrParam = {0}, ObjParam1 = {1}, ObjParam2 = {2}, ObjParam3 = {3})",
                    StrParam, ObjParam1, ObjParam2, ObjParam3)
            };

            BgThread.Start();
        }

        /// <summary>
        /// Raises the <see cref="Complete"/> event for the current background request.
        /// </summary>
        private void InvokeOnComplete()
        {
            if (Complete != null)
                Complete(this);
        }

        /// <summary>
        /// Raises the <see cref="Errored"/> event for the current background request.
        /// </summary>
        private void InvokeOnError()
        {
            if (Errored != null)
                Errored(this);
        }

        /// <summary>
        /// Sends form data to the specified URL on a background thread.
        /// </summary>
        /// <param name="url">
        /// The URL to which the form data will be submitted.
        /// </param>
        /// <param name="postvars">
        /// The form values to include in the request.
        /// </param>
        public void PostData(string url, NameValueCollection postvars)
        {
            StrParam = url;
            ObjParam1 = postvars;

            InitThread(PostDataFunc);
        }

        /// <summary>
        /// Executes an HTTP POST request on the background worker thread.
        /// </summary>
        /// <remarks>
        /// This method is executed by the worker thread created by
        /// <see cref="PostData(string, NameValueCollection)"/>.
        /// </remarks>
        private void PostDataFunc()
        {
            try
            {
                Result = Tools.PostData((NameValueCollection)ObjParam1, StrParam);
                InvokeOnComplete();
            }
            catch (Exception e)
            {
                ErrorException = e;
                InvokeOnError();
            }
        }

        /// <summary>
        /// Executes the specified delegate on a background worker thread.
        /// </summary>
        /// <param name="d">
        /// The delegate to execute asynchronously.
        /// </param>
        public void Execute(ExecuteFunctionDelegate d)
        {
            _cancellationRequested = false;

            BgThread = new Thread(ExecuteFunc)
            {
                Name = "BackgroundThread",
                IsBackground = true
            };

            BgThread.Start(d);
        }

        /// <summary>
        /// Executes the supplied delegate and raises the appropriate completion
        /// or error event when the operation finishes.
        /// </summary>
        /// <param name="d">
        /// The delegate passed to <see cref="Execute(ExecuteFunctionDelegate)"/>.
        /// </param>
        private void ExecuteFunc(object d)
        {
            try
            {
                ((ExecuteFunctionDelegate)d)();
                InvokeOnComplete();
            }
            catch (Exception e)
            {
                ErrorException = e;
                InvokeOnError();
            }
        }

        /// <summary>
        /// Bypasses all redirects in the article
        /// </summary>
        public void BypassRedirects(string article, IApiEdit editor)
        {
            Result = StrParam = article;
            ObjParam1 = editor;

            if (HasUI)
            {
                UI = new PleaseWait();
                UI.CancelRequested += PleaseWaitCancelRequested;
                UI.Show(Variables.MainForm as Form);
            }

            InitThread(BypassRedirectsFunc);
        }

        /// <summary>
        /// checks wikilinks to make them bypass redirects 
        /// </summary>
        private void BypassRedirectsFunc()
        {
            // checks links to make them bypass redirects and (TODO) disambigs
            Dictionary<string, string> knownLinks = new Dictionary<string, string>();

            IApiEdit editor = ObjParam1 as IApiEdit;

            if (editor == null)
            {
                Result = "";
                InvokeOnError();
                return;
            }

            try
            {
                if (HasUI) UI.Status = "Loading links";

                MatchCollection links = WikiRegexes.WikiLinksOnlyPossiblePipe.Matches(StrParam);

                if (HasUI)
                {
                    UI.Status = "Processing links";

                    UI.SetProgress(0, links.Count);
                }
                int n = 0;

                foreach (Match m in links)
                {
                    string link = m.Value;
                    string article = m.Groups[1].Value.TrimStart(new[] { ':' });

                    // if the link is unpiped, use the target as the new link's pipe text
                    string linkText = (!string.IsNullOrEmpty(m.Groups[2].Value)) ? m.Groups[2].Value : article;

                    string ftu = Tools.TurnFirstToUpper(article);

                    string value;
                    if (!knownLinks.TryGetValue(ftu, out value))
                    {
                        // get text
                        string text;
                        try
                        {
                            text = editor.Open(article, false); //TODO:Resolve redirects betterer
                        }
                        catch
                        {
                            continue;
                        }

                        string dest = article;

                        // test if redirect
                        if (Tools.IsRedirect(text))
                        {
                            dest = WebUtility.UrlDecode(Tools.RedirectTarget(text).Replace("_", " "));
                            string directLink = "[[" + dest + "|" + linkText + "]]";

                            StrParam = StrParam.Replace(link, directLink);
                        }
                        knownLinks.Add(ftu, Tools.TurnFirstToUpper(dest));
                    }
                    else if (value != ftu)
                    {
                        string directLink = "[[" + value + "|" + linkText + "]]";

                        StrParam = StrParam.Replace(link, directLink);
                    }
                    n++;
                    if (HasUI) UI.SetProgress(n, links.Count);
                }

                Result = StrParam;
                InvokeOnComplete();
                // UI.Close();
            }
            catch (Exception e)
            {
                // UI.Close();
                ErrorException = e;
                InvokeOnError();
            }
        }

        /// <summary>
        /// Returns a list of articles using GetLists.FromVariant
        /// </summary>
        /// <param name="what">Which source to use</param>
        /// <param name="params1">Optional parameters, depend on source</param>
        public void GetList(IListProvider what, params string[] params1)
        {
            ObjParam1 = what;
            ObjParam2 = params1;

            if (HasUI)
            {
                UI = new PleaseWait();
                UI.CancelRequested += PleaseWaitCancelRequested;
                UI.Show(Variables.MainForm as Form);
            }
            InitThread(GetListFunc);
        }

        /// <summary>
        /// Retrieves a list of pages from the configured <see cref="IListProvider"/>
        /// on the background worker thread.
        /// </summary>
        /// <remarks>
        /// This method is executed by the worker thread created by
        /// <see cref="GetList(IListProvider, string[])"/>. The resulting page list
        /// is stored in <see cref="Result"/> before the completion event is raised.
        /// </remarks>
        private void GetListFunc()
        {
            if (HasUI)
            {
                UI.Status = "Getting list of pages";
            }

            try
            {
                if (_cancellationRequested)
                    return;

                Result = ((IListProvider)ObjParam1).MakeList((string[])ObjParam2);

                if (_cancellationRequested)
                    return;

                InvokeOnComplete();
            }
            catch (Exception e)
            {
                ErrorException = e;
                InvokeOnError();
            }
        }
    }

    /// <summary>
    /// Thread-safe Queue-style container. Supports multiple writers and single reader.
    /// </summary>
    /// <typeparam name="T">Type to store</typeparam>
    public class CrossThreadQueue<T>
    {
        private readonly Queue<T> Queue = new Queue<T>();

        public void Add(T value)
        {
            lock (Queue)
            {
                Queue.Enqueue(value);
            }
        }

        public T Remove()
        {
            lock (Queue)
            {
                return Queue.Dequeue();
            }
        }

        public int Count
        {
            get
            {
                lock (Queue)
                {
                    return Queue.Count;
                }
            }
        }
    }
}