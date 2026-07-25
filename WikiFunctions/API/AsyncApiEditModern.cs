/*
Copyright (C) 2026

Transitional task-based replacement for AsyncApiEdit.

Important:
- This class intentionally exists alongside the legacy AsyncApiEdit class.
- Do not replace existing callers yet.
- This first version wraps the existing synchronous ApiEdit methods in Tasks.
// Cancellation is cooperative and is checked before the synchronous ApiEdit
// call begins. Modern operations also pass the operation token into ApiEdit's
// scoped transport-cancellation path so in-progress HTTP requests can be
// aborted when the request is still active. If an API response has already
// completed successfully, the completed result is preserved.
*/

using System.Threading;

namespace WikiFunctions.API;

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
    private readonly IAsyncApiEditModernOperations ApiOperations;

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
        : this(
            editor,
            callbackContext,
            new ApiEditModernOperations())
    {
    }

    /// <summary>
    /// Internal constructor used by tests to supply controlled ApiEdit behavior.
    ///
    /// Production callers use the public constructor above, which supplies the
    /// normal ApiEditModernOperations adapter.
    /// </summary>
    internal AsyncApiEditModern(
        ApiEdit editor,
        SynchronizationContext callbackContext,
        IAsyncApiEditModernOperations apiOperations)
    {
        if (editor == null)
            throw new ArgumentNullException("editor");

        if (apiOperations == null)
            throw new ArgumentNullException("apiOperations");

        SynchronousEditor = editor;
        CallbackContext = callbackContext;
        ApiOperations = apiOperations;
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

    private bool SetStateWithoutNotification(EditState value)
    {
        lock (SyncRoot)
        {
            if (mState == value)
                return false;

            mState = value;
            return true;
        }
    }

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
            if (SetStateWithoutNotification(value))
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
    ///
    /// Cloning is allowed only when no operation is using the shared underlying
    /// ApiEdit instance. The returned clone has its own operation gate,
    /// cancellation state, and active-operation tracking.
    /// </summary>
    public AsyncApiEditModern Clone()
    {
        // Use the same gate as asynchronous operations. This prevents cloning
        // the underlying ApiEdit while a worker thread may be changing its
        // session, page, user, token, or other mutable state.
        if (!OperationGate.Wait(0))
        {
            throw new InvocationException(
                "Cannot clone AsyncApiEditModern while an operation is active.");
        }

        try
        {
            return new AsyncApiEditModern(
                ApiOperations.Clone(SynchronousEditor),
                CallbackContext,
                ApiOperations);
        }
        finally
        {
            // Always release the gate, including if ApiEdit.Clone() throws.
            OperationGate.Release();
        }
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
                return ApiOperations.Open(SynchronousEditor, title, resolveRedirects, token);
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
                return ApiOperations.Preview(SynchronousEditor, title, text, token);
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
                return ApiOperations.Save(
                    SynchronousEditor,
                    pageText,
                    summary,
                    minor,
                    watch,
                    contentModel,
                    token);
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
                ApiOperations.Login(SynchronousEditor, username, password, token);
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
                ApiOperations.Logout(SynchronousEditor, token);
            },
            cancellationToken);
    }

    public Task WatchAsync(string title)
    {
        return WatchAsync(
            title,
            CancellationToken.None);
    }

    public Task WatchAsync(
        string title,
        CancellationToken cancellationToken)
    {
        return RunOperation(
            "Watch",
            delegate (CancellationToken token)
            {
                ApiOperations.Watch(
                    SynchronousEditor,
                    title,
                    token);
            },
            cancellationToken);
    }

    public Task UnwatchAsync(string title)
    {
        return UnwatchAsync(
            title,
            CancellationToken.None);
    }

    public Task UnwatchAsync(
        string title,
        CancellationToken cancellationToken)
    {
        return RunOperation(
            "Unwatch",
            delegate (CancellationToken token)
            {
                ApiOperations.Unwatch(
                    SynchronousEditor,
                    title,
                    token);
            },
            cancellationToken);
    }

    public Task<string> HttpGetAsync(string url)
    {
        return HttpGetAsync(
            url,
            CancellationToken.None);
    }

    public Task<string> HttpGetAsync(
        string url,
        CancellationToken cancellationToken)
    {
        return RunOperation<string>(
            "HttpGet",
            delegate (CancellationToken token)
            {
                return ApiOperations.HttpGet(
                    SynchronousEditor,
                    url,
                    token);
            },
            cancellationToken);
    }

    public Task<string> ExpandTemplatesAsync(
        string title,
        string text)
    {
        return ExpandTemplatesAsync(
            title,
            text,
            CancellationToken.None);
    }

    public Task<string> ExpandTemplatesAsync(
        string title,
        string text,
        CancellationToken cancellationToken)
    {
        return RunOperation<string>(
            "ExpandTemplates",
            delegate (CancellationToken token)
            {
                return ApiOperations.ExpandTemplates(
                    SynchronousEditor,
                    title,
                    text,
                    token);
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
                ApiOperations.QueryApi(SynchronousEditor, queryParameters, token);
            },
            cancellationToken);
    }
    public Task<string> ParseApiAsync(
        string queryParameters)
    {
        return ParseApiAsync(
            queryParameters,
            CancellationToken.None);
    }

    public Task<string> ParseApiAsync(
        string queryParameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(queryParameters))
            throw new ArgumentException(
                "queryParameters cannot be null or empty.",
                "queryParameters");

        return RunOperation<string>(
            "ParseApi",
            delegate (CancellationToken token)
            {
                return ApiOperations.ParseApi(
                    SynchronousEditor,
                    queryParameters,
                    token);
            },
            cancellationToken);
    }

    /// <summary>
    /// Starts an asynchronous Parse API request using the supplied query
    /// parameters and no cancellation token.
    /// </summary>
    /// <param name="queryParameters">
    /// MediaWiki API query parameters to submit.
    /// </param>
    /// <returns>
    /// A task that completes with the raw Parse API response.
    /// </returns>
    public Task<string> ParseApiAsync(
        Dictionary<string, string> queryParameters) =>
        ParseApiAsync(
            queryParameters,
            CancellationToken.None);

    /// <summary>
    /// Starts an asynchronous Parse API request using the supplied query
    /// parameters and cancellation token.
    /// </summary>
    /// <param name="queryParameters">
    /// MediaWiki API query parameters to submit.
    /// </param>
    /// <param name="cancellationToken">
    /// Token used to cancel the request before completion.
    /// </param>
    /// <returns>
    /// A task that completes with the raw Parse API response.
    /// </returns>
    /// <remarks>
    /// This overload routes the request through <c>RunOperation</c> so the
    /// operation is tracked consistently with other asynchronous editor
    /// actions, ensuring centralized exception handling, status management,
    /// and cancellation behavior.
    /// </remarks>
    public Task<string> ParseApiAsync(
        Dictionary<string, string> queryParameters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        return RunOperation(
            "ParseApi",
            token => ApiOperations.ParseApi(
                SynchronousEditor,
                queryParameters,
                token),
            cancellationToken);
    }

    public Task RollbackAsync(
        string title,
        string user)
    {
        return RollbackAsync(
            title,
            user,
            CancellationToken.None);
    }

    public Task RollbackAsync(
        string title,
        string user,
        CancellationToken cancellationToken)
    {
        return RunOperation(
            "Rollback",
            delegate (CancellationToken token)
            {
                ApiOperations.Rollback(
                    SynchronousEditor,
                    title,
                    user,
                    token);
            },
            cancellationToken);
    }

    public Task MoveAsync(
        string title,
        string newTitle,
        string reason)
    {
        return MoveAsync(
            title,
            newTitle,
            reason,
            true,
            false,
            false,
            CancellationToken.None);
    }

    public Task MoveAsync(
        string title,
        string newTitle,
        string reason,
        bool moveTalk,
        bool noRedirect,
        bool watch,
        CancellationToken cancellationToken)
    {
        return RunOperation(
            "Move",
            delegate (CancellationToken token)
            {
                ApiOperations.Move(
                    SynchronousEditor,
                    title,
                    newTitle,
                    reason,
                    moveTalk,
                    noRedirect,
                    watch,
                    token);
            },
            cancellationToken);
    }

    public Task DeleteAsync(
        string title,
        string reason)
    {
        return DeleteAsync(
            title,
            reason,
            false,
            CancellationToken.None);
    }

    public Task DeleteAsync(
        string title,
        string reason,
        bool watch,
        CancellationToken cancellationToken)
    {
        return RunOperation(
            "Delete",
            delegate (CancellationToken token)
            {
                ApiOperations.Delete(
                    SynchronousEditor,
                    title,
                    reason,
                    watch,
                    token);
            },
            cancellationToken);
    }

    public Task ProtectAsync(
        string title,
        string reason,
        string expiry,
        string edit,
        string move)
    {
        return ProtectAsync(
            title,
            reason,
            expiry,
            edit,
            move,
            false,
            false,
            CancellationToken.None);
    }

    public Task ProtectAsync(
        string title,
        string reason,
        string expiry,
        string edit,
        string move,
        bool cascade,
        bool watch,
        CancellationToken cancellationToken)
    {
        return RunOperation(
            "Protect",
            delegate (CancellationToken token)
            {
                ApiOperations.Protect(
                    SynchronousEditor,
                    title,
                    reason,
                    expiry,
                    edit,
                    move,
                    cascade,
                    watch,
                    token);
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
                ApiOperations.RefreshUserInfo(SynchronousEditor, token);
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
    /// Cancellation prevents work that has not yet started and requests cancellation
    /// of any active ApiEdit transport work that participates in scoped cancellation.
    /// A completed operation still reports its actual success or failure result.
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
    /// Resets the underlying editor when no asynchronous operation owns it.
    ///
    /// Reset uses the same operation gate as OpenAsync, PreviewAsync, SaveAsync,
    /// and the other asynchronous methods. This makes the availability check and
    /// the reset action one atomic operation rather than relying on IsActive,
    /// which is only a moment-in-time snapshot.
    /// </summary>
    public void Reset()
    {
        // Do not wait here. Existing behavior rejects reset while work is active,
        // and callers can still cancel, await WaitAsync(), then call Reset().
        if (!OperationGate.Wait(0))
        {
            throw new InvocationException(
                "Cannot reset AsyncApiEditModern while an operation is active. " +
                "Call CancelCurrentOperation(), await WaitAsync(), and then reset.");
        }

        bool stateChanged = false;

        try
        {
            ApiOperations.Reset(SynchronousEditor);

            // Update state while the gate is still held, but delay notification
            // until after the gate is released. This prevents an event handler
            // from attempting a new operation before reset has fully completed.
            stateChanged = SetStateWithoutNotification(EditState.Ready);
        }
        finally
        {
            // Always release the gate, including if SynchronousEditor.Reset()
            // throws an exception.
            OperationGate.Release();
        }

        // Raise the event only after reset no longer owns the operation gate.
        if (stateChanged)
            RaiseStateChanged();
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

        if (cancellationToken.IsCancellationRequested)
            return CreateCanceledTask<TResult>();

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
                   // Cancellation is honored before the synchronous ApiEdit operation
                   // begins. This prevents a canceled operation from being started.
                   linkedCancellation.Token.ThrowIfCancellationRequested();

                   TResult result = operation(linkedCancellation.Token);

                   // Do not check cancellation again here.
                   //
                   // ApiEdit operations now receive the operation token through the
                   // operations adapter, and the transport layer can observe cancellation
                   // while a request is still active.
                   //
                   // If the synchronous ApiEdit call returned successfully, preserve that
                   // actual result. This is especially important for SaveAsync: the edit
                   // might already be live even if cancellation was requested late.
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
            bool stateChanged = SetStateWithoutNotification(EditState.Failed);

            ReleaseOperationSlot(completion.Task, linkedCancellation);

            completion.TrySetException(ex);

            if (stateChanged)
                RaiseStateChanged();

            ReportFailure(operationName, ex);

            return completion.Task;
        }
    }

    private void CompleteOperation<TResult>(
        string operationName,
        Task<TResult> worker,
        TaskCompletionSource<TResult> completion,
        CancellationTokenSource linkedCancellation)
    {
        // These variables capture the final outcome of the worker task.
        // We do this first so we can decide exactly how the operation ended
        // before we change state, release the gate, or notify callers.
        bool cancelled = worker.IsCanceled;
        Exception failure = null;
        TResult result = default(TResult);

        // If the worker was not already marked as canceled, inspect whether it
        // faulted or succeeded.
        if (!cancelled)
        {
            if (worker.IsFaulted)
            {
                // Unwrap the worker exception into the most useful failure object.
                failure = GetOperationException(worker.Exception);

                // If the underlying failure was really cancellation, treat it as
                // cancellation instead of a normal fault.
                if (failure is OperationCanceledException)
                    cancelled = true;
            }
            else
            {
                // The worker completed successfully, so capture its result.
                result = worker.Result;
            }
        }

        // Decide the final terminal state of this operation.
        // We compute it once here so the rest of the method can follow
        // a single clear completion path.
        EditState finalState;

        if (cancelled)
            finalState = EditState.Aborted;
        else if (failure != null)
            finalState = EditState.Failed;
        else
            finalState = EditState.Ready;

        // IMPORTANT:
        // Commit the final state *before* releasing the operation gate.
        //
        // Why this matters:
        // In the old version, the gate was released first. That allowed a new
        // operation to start and set State = Working, after which the old
        // operation could still come along and overwrite the state with Ready,
        // Failed, or Aborted.
        //
        // By setting the terminal state first, we make sure the old operation is
        // fully finished from a state perspective before another one can begin.
        bool stateChanged = SetStateWithoutNotification(finalState);

        // Now that the final state is committed, release the operation slot.
        // This allows a new operation to begin safely.
        ReleaseOperationSlot(completion.Task, linkedCancellation);

        // Complete the public task and then raise notifications.
        //
        // We keep this ordering deliberate:
        // 1. finalize internal state
        // 2. release the gate
        // 3. complete the task
        // 4. raise notifications/events
        //
        // This avoids the previous race where an older operation could still
        // change the visible state after a new one had started.
        if (cancelled)
        {
            // Publish cancellation to awaiters.
            completion.TrySetCanceled();

            // Notify listeners that the state changed, if it actually did.
            if (stateChanged)
                RaiseStateChanged();

            // Raise the specific Aborted event for compatibility with older
            // event-based callers.
            RaiseAborted();
            return;
        }

        if (failure != null)
        {
            // Publish the failure to awaiters.
            completion.TrySetException(failure);

            // Notify listeners that the state changed, if it actually did.
            if (stateChanged)
                RaiseStateChanged();

            // Raise/log the failure after the task has been completed.
            ReportFailure(operationName, failure);
            return;
        }
        // Success path: publish the successful result.
        completion.TrySetResult(result);

        // Notify listeners that the state changed, if it actually did.
        if (stateChanged)
            RaiseStateChanged();
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
    private static Task<TResult> CreateCanceledTask<TResult>()
    {
        TaskCompletionSource<TResult> completion =
            new TaskCompletionSource<TResult>();

        completion.SetCanceled();

        return completion.Task;
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
