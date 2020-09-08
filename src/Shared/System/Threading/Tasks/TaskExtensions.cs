namespace System.Threading.Tasks
{
	internal static class TaskExtensions
	{
		public static Task<TDerived> Cast<T, TDerived>(this Task<T> task) where TDerived : T
		{
			TaskCompletionSource<TDerived> taskCompletionSource = new TaskCompletionSource<TDerived>();
			task.ContinueWith(delegate(Task<T> t)
			{
				if (t.IsFaulted)
				{
					taskCompletionSource.TrySetException(t.Exception.InnerExceptions);
				}
				else if (t.IsCanceled)
				{
					taskCompletionSource.TrySetCanceled();
				}
				else
				{
					taskCompletionSource.TrySetResult((TDerived)(object)t.Result);
				}
			}, TaskContinuationOptions.ExecuteSynchronously);
			return taskCompletionSource.Task;
		}
	}
}
