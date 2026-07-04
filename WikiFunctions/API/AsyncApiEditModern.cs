/*
Copyright (C) 2026

Transitional task-based replacement for AsyncApiEdit.

Important:
- This class intentionally exists alongside the legacy AsyncApiEdit class.
- Do not replace existing callers yet.
- This first version wraps the existing synchronous ApiEdit methods in Tasks.
- Cancellation is cooperative and currently checked before and after the
  synchronous ApiEdit call. True in-progress HTTP cancellation will be added
  later when ApiEdit's HTTP layer accepts CancellationToken values.
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace WikiFunctions.API
{
    /// <summary>
    /// Provides details when an AsyncApiEditModern operation fails.
    /// </summary>
    public sealed class AsyncApiEditOperationFailedEventArgs : EventArgs
    {
        public AsyncApiEditOperationFailedEventArgs(
            string operationName,
            Exception exception)
        {
            OperationName = operationName;
            Exception = exception;
        }

        public string OperationName { get; private set; }

        public Exception Exception { get; private set; }
    }

    /// <summary>
    /// Provides maxlag details for a failed API operation.
    /// </summary>
    public sealed class AsyncApiEditMaxlagEventArgs : EventArgs
    {
        public AsyncApiEditMaxlagEventArgs(double maxlag, int retryAfter)
        {
            Maxlag = maxlag;
            RetryAfter = retryAfter;
        }

        public double Maxlag { get; private set; }

        public int RetryAfter { get; private set; }
    }

    /// <summary>
    /// Transitional task-based wrapper around the existing synchronous ApiEdit.
    ///
    /// This class is designed to coexist with the legacy AsyncApiEdit class
    /// until all callers have been migrated.
    /// </summary>
    public class AsyncApiEditModern
    {
        private static readonly Task CompletedTask = CreateCompletedTask();

        private readonly object SyncRoot = new object();
        private readonly SemaphoreSlim OperationGate =
            new SemaphoreSlim(1, 1);

        private readonly SynchronizationContext CallbackContext;

        private CancellationTokenSource ActiveOperationCancellation;
        private Task ActiveOperation = CompletedTask;
        private EditState mState;

        /// <summary>
        /// Creates an editor that posts events to the current synchronization
        /// context, if one exists.
        ///
        /// For WinForms callers, construct this from the UI thread after the
        /// form has initialized, or use the overload that explicitly receives
        /// SynchronizationContext.Current.
        /// </summary>
        public AsyncApiEditModern(string url)
            : this(new ApiEdit(url), SynchronizationContext.Current)
        {
        }

        /// <summary>
        /// Creates an editor that posts events to the supplied synchronization
        /// context. This keeps WikiFunctions independent from WinForms Control.
        /// </summary>
        public AsyncApiEditModern(
            string url,
            SynchronizationContext callbackContext)
            : this(new ApiEdit(url), callbackContext)
        {
        }

        /// <summary>
        /// Creates an editor around an existing ApiEdit instance.
        /// </summary>
        public AsyncApiEditModern(ApiEdit editor)
            : this(editor, SynchronizationContext.Current)
        {
        }

        /// <summary>
        /// Creates an editor around an existing ApiEdit instance and posts
        /// events to the supplied synchronization context.
        /// </summary>
        public AsyncApiEditModern(
            ApiEdit editor,
            SynchronizationContext callbackContext)
        {
            if (editor == null)
                throw new ArgumentNullException("editor");

            SynchronousEditor = editor;
            CallbackContext = callbackContext;
            mState = EditState.Ready;
        }

        /// <summary>
        /// Provides access to the underlying synchronous editor.
        ///
        /// Do not call mutating methods on this object directly while an
        /// AsyncApiEditModern operation is running.
        /// </summary>
        public ApiEdit SynchronousEditor { get; private set; }

        /// <summary>
        /// Represents the current operation state.
        /// </summary>
        public enum EditState
        {
            Ready,
            Working,
            Aborted,
            Failed
        }

        /// <summary>
        /// Raised after the State value changes.
        /// </summary>
        public event EventHandler StateChanged;

        /// <summary>
        /// Raised after an operation has actually observed cancellation and
        /// completed as aborted.
        /// </summary>
        public event EventHandler Aborted;

        /// <summary>
        /// Raised for an ordinary API or runtime failure.
        ///
        /// New callers should normally prefer await plus try/catch. This event
        /// exists only as a transition aid for older event-oriented callers.
        /// </summary>
        public event EventHandler<AsyncApiEditOperationFailedEventArgs>
            OperationFailed;

        /// <summary>
        /// Raised when the API reports maxlag.
        /// </summary>
        public event EventHandler<AsyncApiEditMaxlagEventArgs>
            MaxlagExceeded;

        /// <summary>
        /// Raised when the API reports that the editor is no longer logged in.
        /// </summary>
        public event EventHandler LoggedOff;

        /// <summary>
        /// Gets the current state of the editor.
        /// </summary>
        public EditState State
        {
            get
            {
                lock (SyncRoot)
                {
                    return mState;
                }
            }
            private set
            {
                bool changed;

                lock (SyncRoot)
                {
                    changed = mState != value;
                    mState = value;
                }

                if (changed)
                    RaiseStateChanged();
            }
        }

        /// <summary>
        /// Gets whether an operation is currently in progress.
        /// </summary>
        public bool IsActive
        {
            get
            {
                lock (SyncRoot)
                {
                    return ActiveOperationCancellation != null;
                }
            }
        }

        /// <summary>
        /// Creates a separate task-based editor with a cloned ApiEdit instance.
        /// The clone has its own operation gate and cancellation state.
        /// </summary>
        public AsyncApiEditModern Clone()
        {
            return new AsyncApiEditModern(
                (ApiEdit)SynchronousEditor.Clone(),
                CallbackContext);
        }

        #region ApiEdit property forwarding

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

        public UserInfo User
        {
            get { return SynchronousEditor.User; }
        }

        #endregion

        #region Public task-based operations

        public Task<PageInfo> OpenAsync(
            string title,
            bool resolveRedirects)
        {
            return OpenAsync(
                title,
                resolveRedirects,
                CancellationToken.None);
        }

        public Task<PageInfo> OpenAsync(
            string title,
            bool resolveRedirects,
            CancellationToken cancellationToken)
        {
            return RunOperation<PageInfo>(
                "Open",
                delegate (CancellationToken token)
                {
                    SynchronousEditor.Open(title, resolveRedirects);
                    return SynchronousEditor.Page;
                },
                cancellationToken);
        }

        public Task<string> PreviewAsync(
            string title,
            string text)
        {
            return PreviewAsync(
                title,
                text,
                CancellationToken.None);
        }

        public Task<string> PreviewAsync(
            string title,
            string text,
            CancellationToken cancellationToken)
        {
            return RunOperation<string>(
                "Preview",
                delegate (CancellationToken token)
                {
                    return SynchronousEditor.Preview(title, text);
                },
                cancellationToken);
        }

        public Task<SaveInfo> SaveAsync(
            string pageText,
            string summary,
            bool minor,
            WatchOptions watch)
        {
            return SaveAsync(
                pageText,
                summary,
                minor,
                watch,
                "wikitext",
                CancellationToken.None);
        }

        public Task<SaveInfo> SaveAsync(
            string pageText,
            string summary,
            bool minor,
            WatchOptions watch,
            string contentModel,
            CancellationToken cancellationToken)
        {
            return RunOperation<SaveInfo>(
                "Save",
                delegate (CancellationToken token)
                {
                    return SynchronousEditor.Save(
                        pageText,
                        summary,
                        minor,
                        watch,
                        contentModel);
                },
                cancellationToken);
        }

        public Task LoginAsync(
            string username,
            string password)
        {
            return LoginAsync(
                username,
                password,
                CancellationToken.None);
        }

        public Task LoginAsync(
            string username,
            string password,
            CancellationToken cancellationToken)
        {
            return RunOperation(
                "Login",
                delegate (CancellationToken token)
                {
                    SynchronousEditor.Login(username, password);
                },
                cancellationToken);
        }

        public Task LogoutAsync()
        {
            return LogoutAsync(CancellationToken.None);
        }

        public Task LogoutAsync(CancellationToken cancellationToken)
        {
            return RunOperation(
                "Logout",
                delegate (CancellationToken token)
                {
                    SynchronousEditor.Logout();
                },
                cancellationToken);
        }

        public Task QueryApiAsync(string queryParameters)
        {
            return QueryApiAsync(
                queryParameters,
                CancellationToken.None);
        }

        public Task QueryApiAsync(
            string queryParameters,
            CancellationToken cancellationToken)
        {
            return RunOperation(
                "QueryApi",
                delegate (CancellationToken token)
                {
                    SynchronousEditor.QueryApi(queryParameters);
                },
                cancellationToken);
        }
                public Task<string> ParseApiAsync(
            Dictionary<string, string> queryParameters)
        {
            return ParseApiAsync(
                queryParameters,
                CancellationToken.None);
        }

        public Task<string> ParseApiAsync(
            Dictionary<string, string> queryParameters,
            CancellationToken cancellationToken)
        {
            if (queryParameters == null)
                throw new ArgumentNullException("queryParameters");

            return RunOperation<string>(
                "ParseApi",
                delegate (CancellationToken token)
                {
                    token.ThrowIfCancellationRequested();

                    return SynchronousEditor.ParseApi(queryParameters);
                },
                cancellationToken);
        }

        public Task RefreshUserInfoAsync()
        {
            return RefreshUserInfoAsync(CancellationToken.None);
        }

        public Task RefreshUserInfoAsync(
            CancellationToken cancellationToken)
        {
            return RunOperation(
                "RefreshUserInfo",
                delegate (CancellationToken token)
                {
                    SynchronousEditor.RefreshUserInfo();
                },
                cancellationToken);
        }

        #endregion

        #region Operation control

        /// <summary>
        /// Returns the currently running operation task.
        ///
        /// New code should normally await OpenAsync, SaveAsync, and similar
        /// methods directly rather than calling WaitAsync separately.
        /// </summary>
        public Task WaitAsync()
        {
            lock (SyncRoot)
            {
                return ActiveOperation;
            }
        }

        /// <summary>
        /// Requests cooperative cancellation of the current operation.
        ///
        /// This does not force-stop a thread and does not claim that the
        /// underlying HTTP request has already ended.
        /// </summary>
        public bool CancelCurrentOperation()
        {
            CancellationTokenSource cancellation;

            lock (SyncRoot)
            {
                cancellation = ActiveOperationCancellation;
            }

            if (cancellation == null || cancellation.IsCancellationRequested)
                return false;

            try
            {
                cancellation.Cancel();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        /// <summary>
        /// Resets the underlying editor only when no operation is running.
        ///
        /// Future work will add ResetAsync so callers can request cancellation,
        /// await completion, and then reset safely.
        /// </summary>
        public void Reset()
        {
            if (IsActive)
            {
                throw new InvocationException(
                    "Cannot reset AsyncApiEditModern while an operation is active. " +
                    "Call CancelCurrentOperation(), await WaitAsync(), and then reset.");
            }

            SynchronousEditor.Reset();
            State = EditState.Ready;
        }

        #endregion

        #region Core operation helpers

        private Task RunOperation(
            string operationName,
            Action<CancellationToken> operation,
            CancellationToken cancellationToken)
        {
            if (operation == null)
                throw new ArgumentNullException("operation");

            return RunOperation<object>(
                operationName,
                delegate (CancellationToken token)
                {
                    operation(token);
                    return null;
                },
                cancellationToken);
        }

        /// <summary>
        /// Runs one synchronous ApiEdit operation on a ThreadPool thread.
        ///
        /// This is intentionally a transitional bridge. It keeps the UI
        /// responsive now, while a later ApiEdit HTTP refactor will replace
        /// this with genuine asynchronous HTTP calls.
        /// </summary>
        private Task<TResult> RunOperation<TResult>(
            string operationName,
            Func<CancellationToken, TResult> operation,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(operationName))
                throw new ArgumentException(
                    "An operation name is required.",
                    "operationName");

            if (operation == null)
                throw new ArgumentNullException("operation");

            cancellationToken.ThrowIfCancellationRequested();

            if (!OperationGate.Wait(0))
            {
                throw new InvocationException(
                    "An asynchronous call is already being performed.");
            }

            CancellationTokenSource linkedCancellation =
                CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken);

            TaskCompletionSource<TResult> completion =
                new TaskCompletionSource<TResult>();

            try
            {
                lock (SyncRoot)
                {
                    ActiveOperationCancellation = linkedCancellation;
                    ActiveOperation = completion.Task;
                }

                State = EditState.Working;

                Task<TResult> worker = Task.Factory.StartNew<TResult>(
                    delegate
                    {
                        linkedCancellation.Token.ThrowIfCancellationRequested();

                        TResult result = operation(linkedCancellation.Token);

                        // This converts a completed synchronous operation into
                        // cancellation if a cancellation request arrived while
                        // the old ApiEdit method was running.
                        linkedCancellation.Token.ThrowIfCancellationRequested();

                        return result;
                    },
                    linkedCancellation.Token,
                    TaskCreationOptions.None,
                    TaskScheduler.Default);

                worker.ContinueWith(
                    delegate (Task<TResult> completedWorker)
                    {
                        CompleteOperation(
                            operationName,
                            completedWorker,
                            completion,
                            linkedCancellation);
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);

                return completion.Task;
            }
            catch (Exception ex)
            {
                ReleaseOperationSlot(completion.Task, linkedCancellation);

                State = EditState.Failed;
                ReportFailure(operationName, ex);

                completion.TrySetException(ex);
                return completion.Task;
            }
        }

        private void CompleteOperation<TResult>(
            string operationName,
            Task<TResult> worker,
            TaskCompletionSource<TResult> completion,
            CancellationTokenSource linkedCancellation)
        {
            bool cancelled = worker.IsCanceled;
            Exception failure = null;
            TResult result = default(TResult);

            if (!cancelled)
            {
                if (worker.IsFaulted)
                {
                    failure = GetOperationException(worker.Exception);

                    if (failure is OperationCanceledException)
                        cancelled = true;
                }
                else
                {
                    result = worker.Result;
                }
            }

            // Release the operation gate before raising events or completing
            // the public task. An event handler or continuation may safely
            // start the next operation.
            ReleaseOperationSlot(completion.Task, linkedCancellation);

            if (cancelled)
            {
                State = EditState.Aborted;
                RaiseAborted();

                completion.TrySetCanceled();
                return;
            }

            if (failure != null)
            {
                State = EditState.Failed;
                ReportFailure(operationName, failure);

                completion.TrySetException(failure);
                return;
            }

            State = EditState.Ready;
            completion.TrySetResult(result);
        }

        private void ReleaseOperationSlot(
            Task completionTask,
            CancellationTokenSource linkedCancellation)
        {
            lock (SyncRoot)
            {
                if (object.ReferenceEquals(
                    ActiveOperationCancellation,
                    linkedCancellation))
                {
                    ActiveOperationCancellation = null;
                }

                if (object.ReferenceEquals(
                    ActiveOperation,
                    completionTask))
                {
                    ActiveOperation = CompletedTask;
                }
            }

            linkedCancellation.Dispose();
            OperationGate.Release();
        }

        private static Exception GetOperationException(
            AggregateException aggregateException)
        {
            AggregateException flattened = aggregateException.Flatten();

            if (flattened.InnerExceptions.Count == 1)
                return flattened.InnerExceptions[0];

            return flattened;
        }

        private static Task CreateCompletedTask()
        {
            TaskCompletionSource<object> completion =
                new TaskCompletionSource<object>();

            completion.SetResult(null);

            return completion.Task;
        }

        #endregion

        #region Event helpers

        private void RaiseStateChanged()
        {
            PostCallback(
                delegate
                {
                    EventHandler handler = StateChanged;

                    if (handler != null)
                        handler(this, EventArgs.Empty);
                });
        }

        private void RaiseAborted()
        {
            PostCallback(
                delegate
                {
                    EventHandler handler = Aborted;

                    if (handler != null)
                        handler(this, EventArgs.Empty);
                });
        }

        private void ReportFailure(
            string operationName,
            Exception exception)
        {
            Tools.WriteDebug(
                "ApiEdit",
                operationName + " failed: " + exception);

            MaxlagException maxlagException =
                exception as MaxlagException;

            if (maxlagException != null)
            {
                RaiseMaxlagExceeded(
                    maxlagException.Maxlag,
                    maxlagException.RetryAfter);

                return;
            }

            if (exception is LoggedOffException)
            {
                RaiseLoggedOff();
                return;
            }

            RaiseOperationFailed(operationName, exception);
        }

        private void RaiseOperationFailed(
            string operationName,
            Exception exception)
        {
            PostCallback(
                delegate
                {
                    EventHandler<AsyncApiEditOperationFailedEventArgs>
                        handler = OperationFailed;

                    if (handler != null)
                    {
                        handler(
                            this,
                            new AsyncApiEditOperationFailedEventArgs(
                                operationName,
                                exception));
                    }
                });
        }

        private void RaiseMaxlagExceeded(
            double maxlag,
            int retryAfter)
        {
            PostCallback(
                delegate
                {
                    EventHandler<AsyncApiEditMaxlagEventArgs>
                        handler = MaxlagExceeded;

                    if (handler != null)
                    {
                        handler(
                            this,
                            new AsyncApiEditMaxlagEventArgs(
                                maxlag,
                                retryAfter));
                    }
                });
        }

        private void RaiseLoggedOff()
        {
            PostCallback(
                delegate
                {
                    EventHandler handler = LoggedOff;

                    if (handler != null)
                        handler(this, EventArgs.Empty);
                });
        }

        private void PostCallback(Action callback)
        {
            if (CallbackContext == null)
            {
                InvokeCallbackSafely(callback);
                return;
            }

            CallbackContext.Post(
                delegate (object ignored)
                {
                    InvokeCallbackSafely(callback);
                },
                null);
        }

        private static void InvokeCallbackSafely(Action callback)
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                Tools.WriteDebug(
                    "ApiEdit",
                    "AsyncApiEditModern callback failed: " + ex);
            }
        }

        #endregion
    }
}
