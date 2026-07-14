/*
Copyright (C) 2008 Max Semenik

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
using System.Threading;
using System.Windows.Forms;

namespace WikiFunctions.API
{
    public delegate void AsyncEventHandler(AsyncApiEdit sender);

    public delegate void AsyncOpenEditHandler(AsyncApiEdit sender, PageInfo pageInfo);

    public delegate void AsyncSaveEventHandler(AsyncApiEdit sender, SaveInfo saveInfo);

    public delegate void AsyncStringEventHandler(AsyncApiEdit sender, string result);

    public delegate void AsyncExceptionEventHandler(AsyncApiEdit sender, Exception ex);

    public delegate void AsyncMaxlagEventHandler(AsyncApiEdit sender, double maxlag, int retryAfter);

    /// <summary>
    /// Multithreaded API editor class
    /// </summary>
    public class AsyncApiEdit
    {
        private readonly Control ParentControl;
        private readonly AsyncApiEditModern ModernEditor;
        private readonly object ModernSyncRoot = new object();

        private Task ActiveModernOperation;
        private CancellationTokenSource ActiveModernCancellation;
        private bool InCrossThreadCall;

        public AsyncApiEdit(string url)
            : this(url, null)
        {
        }

        public AsyncApiEdit(string url, Control parentControl)
            : this(new ApiEdit(url), parentControl)
        {
        }

        private AsyncApiEdit(ApiEdit editor, Control parentControl)
        {
            SynchronousEditor = editor;
            ParentControl = parentControl;
            ModernEditor = new AsyncApiEditModern(editor, null);
            State = EditState.Ready;
        }

        public AsyncApiEdit Clone()
        {
            return new AsyncApiEdit((ApiEdit)SynchronousEditor.Clone(), ParentControl);
        }

        /// <summary>
        /// Provides access to the underlying ApiEdit
        /// </summary>
        public ApiEdit SynchronousEditor { get; private set; }

        public enum EditState
        {
            /// <summary>
            /// Nothing goes on, last operation completed successfully
            /// </summary>
            Ready,

            /// <summary>
            /// The editor is performing a background operation
            /// </summary>
            Working,

            /// <summary>
            /// Operation aborted
            /// </summary>
            Aborted,

            /// <summary>
            /// Last operation ended unsuccessfully
            /// </summary>
            Failed
        }

        private EditState mState = EditState.Ready;

        /// <summary>
        /// State of the editor
        /// </summary>
        public EditState State
        {
            get { return mState; }
            protected set
            {
                CallEvent(StateChanged, this);
                mState = value;
            }
        }

        /// <summary>
        /// True if we are currently performing an operation
        /// </summary>
        public bool IsActive
        {
            get { return State == EditState.Working; }
        }

        /// <summary>
        /// Waits for asynchronous operation to complete
        /// </summary>
        public void Wait()
        {
            Task modernOperation;

            lock (ModernSyncRoot)
            {
                modernOperation = ActiveModernOperation;
            }

            if (modernOperation == null)
                return;

            if (ParentControl != null && !ParentControl.InvokeRequired)
            {
                while (IsModernOperationActive)
                    Application.DoEvents();
            }
            else
            {
                try
                {
                    modernOperation.Wait();
                }
                catch (AggregateException)
                {
                    // Modern operation failures are reported through the existing
                    // legacy completion/failure event path.
                }
            }
        }

        #region Events

        public event AsyncOpenEditHandler OpenComplete;
        public event AsyncSaveEventHandler SaveComplete;
        public event AsyncStringEventHandler PreviewComplete;

        public event AsyncExceptionEventHandler ExceptionCaught;
        public event AsyncMaxlagEventHandler MaxlagExceeded;
        public event AsyncEventHandler LoggedOff;

        public event AsyncEventHandler StateChanged;

        public event AsyncEventHandler Aborted;

        #endregion

        #region Events internal

        private delegate void OperationEndedInternal(string operation, object result);

        private delegate void OperationFailedInternal(string operation, Exception ex);

        private delegate void ExceptionCaughtInternal(Exception ex);

        protected virtual void OnOperationComplete(string operation, object result)
        {
            switch (operation)
            {
                case "Open":
                    if (OpenComplete != null) OpenComplete(this, Page);
                    break;
                case "Save":
                    if (SaveComplete != null) SaveComplete(this, (SaveInfo)result);
                    break;
                case "Preview":
                    if (PreviewComplete != null) PreviewComplete(this, (string)result);
                    break;
            }
        }

        protected virtual void OnOperationFailed(string operation, Exception ex)
        {
            Tools.WriteDebug("ApiEdit", ex.Message);

            if (ex is MaxlagException)
            {
                var exm = (MaxlagException)ex;
                if (MaxlagExceeded != null) MaxlagExceeded(this, exm.Maxlag, exm.RetryAfter);
            }
            else if (ex is LoggedOffException)
            {
                if (LoggedOff != null) LoggedOff(this);
            }

            else
                OnExceptionCaught(ex);
        }

        protected virtual void OnExceptionCaught(Exception ex)
        {
            if (ExceptionCaught != null) ExceptionCaught(this, ex);
        }

        #endregion

        #region Death magic invocations

        /// <summary>
        /// Invokes a supplied delegate. If the editor is owned by a control, the
        /// delegate will called from the control's thread, otherwise it will be 
        /// called from current thread.
        /// </summary>
        private void CallEvent(Delegate method, params object[] args)
        {
            if (method == null) return;

            if (ParentControl == null)
            {
                method.DynamicInvoke(args);
            }
            else
            {
                InCrossThreadCall = true;
                if (!ParentControl.IsDisposed)
                {
                    ParentControl.Invoke(method, args);
                }
                InCrossThreadCall = false;
            }
        }

        private bool IsModernOperationActive
        {
            get
            {
                lock (ModernSyncRoot)
                {
                    return ActiveModernOperation != null &&
                           !ActiveModernOperation.IsCompleted;
                }
            }
        }

        private delegate Task ModernActionFactory(
            CancellationToken cancellationToken);

        private delegate Task<TResult> ModernOperationFactory<TResult>(
            CancellationToken cancellationToken);

        private void InvokeModernAction(
            string operation,
            ModernActionFactory operationFactory)
        {
            if (operationFactory == null)
                throw new ArgumentNullException("operationFactory");

            InvokeModernFunction<object>(
                operation,
                delegate (CancellationToken cancellationToken)
                {
                    Task actionTask = operationFactory(cancellationToken);

                    if (actionTask == null)
                        throw new InvalidOperationException(
                            "Modern operation did not return a task.");

                    return ConvertModernActionTask(actionTask);
                });
        }

        private static Task<object> ConvertModernActionTask(Task actionTask)
        {
            TaskCompletionSource<object> completion =
                new TaskCompletionSource<object>();

            actionTask.ContinueWith(
                delegate (Task completedTask)
                {
                    if (completedTask.IsCanceled)
                    {
                        completion.TrySetCanceled();
                        return;
                    }

                    if (completedTask.IsFaulted)
                    {
                        completion.TrySetException(
                            completedTask.Exception.InnerExceptions);

                        return;
                    }

                    completion.TrySetResult(null);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return completion.Task;
        }

        private void InvokeModernFunction<TResult>(
            string operation,
            ModernOperationFactory<TResult> operationFactory)
        {
            if (operationFactory == null)
                throw new ArgumentNullException("operationFactory");

            if (IsModernOperationActive)
            {
                throw new InvocationException(
                    "An asynchronous call is already being performed");
            }

            CancellationTokenSource cancellation =
                new CancellationTokenSource();

            State = EditState.Working;

            Task<TResult> task;

            try
            {
                task = operationFactory(cancellation.Token);

                if (task == null)
                    throw new InvalidOperationException(
                        "Modern operation did not return a task.");
            }
            catch (Exception ex)
            {
                cancellation.Dispose();

                SynchronousEditor.Reset();

                State = EditState.Failed;
                CallModernFailure(operation, ex);
                return;
            }

            lock (ModernSyncRoot)
            {
                ActiveModernCancellation = cancellation;
                ActiveModernOperation = task;
            }

            task.ContinueWith(
                delegate (Task<TResult> completedTask)
                {
                    CompleteModernOperation(
                        operation,
                        completedTask,
                        cancellation);
                },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private void CompleteModernOperation<TResult>(
            string operation,
            Task<TResult> completedTask,
            CancellationTokenSource cancellation)
        {
            try
            {
                bool cancelled = completedTask.IsCanceled;
                Exception failure = null;
                TResult result = default(TResult);

                if (!cancelled)
                {
                    if (completedTask.IsFaulted)
                    {
                        failure = GetModernTaskException(completedTask.Exception);

                        if (failure is OperationCanceledException)
                            cancelled = true;
                    }
                    else
                    {
                        result = completedTask.Result;
                    }
                }

                ClearModernOperation(completedTask, cancellation);

                if (cancelled)
                {
                    State = EditState.Aborted;

                    if (Aborted != null)
                        CallEvent(Aborted, this);

                    return;
                }

                if (failure != null)
                {
                    SynchronousEditor.Reset();

                    State = EditState.Failed;
                    CallModernFailure(operation, failure);
                    return;
                }

                State = EditState.Ready;

                // No state changes past this point; the callback may launch another operation.
                CallEvent(
                    new OperationEndedInternal(OnOperationComplete),
                    operation,
                    result);
            }
            catch (Exception ex)
            {
                ClearModernOperation(completedTask, cancellation);

                try
                {
                    SynchronousEditor.Reset();
                }
                catch
                {
                }

                State = EditState.Failed;
                CallModernFailure(operation, ex);
            }
        }

        private void CallModernFailure(
            string operation,
            Exception exception)
        {
            if (operation != null && exception is ApiException)
            {
                CallEvent(
                    new OperationFailedInternal(OnOperationFailed),
                    operation,
                    exception);
            }
            else
            {
                CallEvent(
                    new ExceptionCaughtInternal(OnExceptionCaught),
                    exception);
            }
        }

        private void ClearModernOperation(
            Task completedTask,
            CancellationTokenSource cancellation)
        {
            lock (ModernSyncRoot)
            {
                if (object.ReferenceEquals(
                    ActiveModernOperation,
                    completedTask))
                {
                    ActiveModernOperation = null;
                }

                if (object.ReferenceEquals(
                    ActiveModernCancellation,
                    cancellation))
                {
                    ActiveModernCancellation = null;
                }
            }

            try
            {
                cancellation.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private static Exception GetModernTaskException(
            AggregateException aggregateException)
        {
            AggregateException flattened = aggregateException.Flatten();

            if (flattened.InnerExceptions.Count == 1)
                return flattened.InnerExceptions[0];

            return flattened;
        }

        #endregion

        #region IApiEdit Members

        public string URL
        {
            get { return SynchronousEditor.URL; }
        }

        public string ApiURL
        {
            get { return SynchronousEditor.ApiURL; }
        }

        public bool PHP5
        {
            get { return SynchronousEditor.PHP5; }
        }

        public int Maxlag
        {
            get { return SynchronousEditor.Maxlag; }
            set { SynchronousEditor.Maxlag = value; }
        }

        public bool NewMessageThrows
        {
            get { return SynchronousEditor.NewMessageThrows; }
            set { SynchronousEditor.NewMessageThrows = value; }
        }

        public string Action
        {
            get { return SynchronousEditor.Action; }
        }

        public string HtmlHeaders
        {
            get { return SynchronousEditor.HtmlHeaders; }
        }

        public PageInfo Page
        {
            get { return SynchronousEditor.Page; }
        }

        public void Reset()
        {
            Abort();
            SynchronousEditor.Reset();
        }

        public void HttpGet(string url)
        {
            InvokeModernFunction<string>(
                "HttpGet",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.HttpGetAsync(
                        url,
                        cancellationToken);
                });
        }

        public void Login(string username, string password)
        {
            InvokeModernAction(
                "Login",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.LoginAsync(
                        username,
                        password,
                        cancellationToken);
                });
        }

        public void Logout()
        {
            InvokeModernAction(
                "Logout",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.LogoutAsync(cancellationToken);
                });
        }

        public void Open(string title, bool resolveRedirects)
        {
            InvokeModernFunction<PageInfo>(
                "Open",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.OpenAsync(
                        title,
                        resolveRedirects,
                        cancellationToken);
                });
        }

        public void Save(string pageText, string summary, bool minor, WatchOptions watch, string contentModel = "wikitext")
        {
            InvokeModernFunction<SaveInfo>(
                "Save",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.SaveAsync(
                        pageText,
                        summary,
                        minor,
                        watch,
                        contentModel,
                        cancellationToken);
                });
        }

        public void Watch(string title)
        {
            InvokeModernAction(
                "Watch",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.WatchAsync(
                        title,
                        cancellationToken);
                });
        }

        public void Unwatch(string title)
        {
            InvokeModernAction(
                "Unwatch",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.UnwatchAsync(
                        title,
                        cancellationToken);
                });
        }

        public void Delete(string title, string reason)
        {
            Delete(title, reason, false);
        }

        public void Delete(string title, string reason, bool watch)
        {
            InvokeModernAction(
                "Delete",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.DeleteAsync(
                        title,
                        reason,
                        watch,
                        cancellationToken);
                });
        }

        public void Protect(string title, string reason, string expiry, string edit, string move, bool cascade,
            bool watch)
        {
            InvokeModernAction(
                "Protect",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.ProtectAsync(
                        title,
                        reason,
                        expiry,
                        edit,
                        move,
                        cascade,
                        watch,
                        cancellationToken);
                });
        }

        public void Protect(string title, string reason, TimeSpan expiry, string edit, string move, bool cascade,
            bool watch)
        {
            Protect(title, reason, expiry.ToString(), edit, move, cascade, watch);
        }

        public void Protect(string title, string reason, string expiry, string edit, string move)
        {
            Protect(title, reason, expiry, edit, move, false, false);
        }

        public void Protect(string title, string reason, TimeSpan expiry, string edit, string move)
        {
            Protect(title, reason, expiry.ToString(), edit, move, false, false);
        }

        public void Move(string title, string newTitle, string reason)
        {
            Move(title, newTitle, reason, true, false, false);
        }

        public void Move(string title, string newTitle, string reason, bool moveTalk, bool noRedirect)
        {
            Move(title, newTitle, reason, moveTalk, noRedirect, false);
        }

        public void Move(string title, string newTitle, string reason, bool moveTalk, bool noRedirect, bool watch)
        {
            InvokeModernAction(
                "Move",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.MoveAsync(
                        title,
                        newTitle,
                        reason,
                        moveTalk,
                        noRedirect,
                        watch,
                        cancellationToken);
                });
        }

        public void Preview(string title, string text)
        {
            InvokeModernFunction<string>(
                "Preview",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.PreviewAsync(
                        title,
                        text,
                        cancellationToken);
                });
        }

        public void QueryApi(string queryParameters)
        {
            InvokeModernAction(
                "QueryApi",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.QueryApiAsync(
                        queryParameters,
                        cancellationToken);
                });
        }

        public void ParseApi(string queryParameters)
        {
            InvokeModernFunction<string>(
                "ParseApi",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.ParseApiAsync(
                        queryParameters,
                        cancellationToken);
                });
        }

        public void Rollback(string title, string user)
        {
            InvokeModernAction(
                "Rollback",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.RollbackAsync(
                        title,
                        user,
                        cancellationToken);
                });
        }

        public void ExpandTemplates(string title, string text)
        {
            InvokeModernFunction<string>(
                "ExpandTemplates",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.ExpandTemplatesAsync(
                        title,
                        text,
                        cancellationToken);
                });
        }

        public void Abort()
        {
            if (InCrossThreadCall) return; // otherwise we'll deadlock

            CancellationTokenSource modernCancellation;

            lock (ModernSyncRoot)
            {
                modernCancellation = ActiveModernCancellation;
            }

            if (modernCancellation != null)
            {
                try
                {
                    modernCancellation.Cancel();
                }
                catch (ObjectDisposedException)
                {
                }

                Wait();
                return;
            }

            if (Aborted != null)
                Aborted(this);

            State = EditState.Aborted;
        }

        #endregion

        #region User info

        public UserInfo User
        {
            get { return SynchronousEditor.User; }
        }

        public void RefreshUserInfo()
        {
            InvokeModernAction(
                "RefreshUserInfo",
                delegate (CancellationToken cancellationToken)
                {
                    return ModernEditor.RefreshUserInfoAsync(cancellationToken);
                });
        }
        #endregion
    }
}
