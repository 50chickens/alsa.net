using System.Runtime.InteropServices;

namespace AlsaSharp.Internal
{
	/// <summary>
	/// Represents the outcome of a native diagnostic invocation including success flag, result and optional error message.
	/// </summary>
	public readonly struct DiagnosticResult<T>
	{
        /// <summary>
        /// Indicates whether a native diagnostic call succeeded and carries the result or error.
        /// </summary>
		public bool Success { get; }
        /// <summary>The result value returned by the operation when <see cref="Success"/> is true.</summary>
		public T Result { get; }
        /// <summary>An error message when <see cref="Success"/> is false; otherwise <c>null</c>.</summary>
		public string? Error { get; }

		/// <summary>Creates a new diagnostic result.</summary>
		/// <param name="success">Whether the operation succeeded.</param>
		/// <param name="result">The operation result value.</param>
		/// <param name="error">Optional error message when <paramref name="success"/> is false.</param>
		public DiagnosticResult(bool success, T result, string? error)
		{
			Success = success;
			Result = result;
			Error = error;
		}
	}

	/// <summary>
	/// Helpers to safely invoke native interop calls and marshal results for diagnostics.
	/// Intended for use in tests and diagnostic code only.
	/// </summary>
	public static class NativeDiagnostics
	{
		/// <summary>Runs the provided action and returns a <see cref="DiagnosticResult{T}"/> capturing exceptions as errors.</summary>
		/// <param name="action">The action to execute.</param>
		/// <param name="name">A short name used for context in error messages.</param>
		public static DiagnosticResult<T> Run<T>(Func<T> action, string name)
		{
			try
			{
				var r = action();
				return new DiagnosticResult<T>(true, r, null);
			}
			catch (Exception ex)
			{
				return new DiagnosticResult<T>(false, default!, ex.ToString());
			}
		}

		/// <summary>
		/// Safely marshals a native UTF8 string pointer to a managed string.
		/// Returns an empty string when <paramref name="ptr"/> is <see cref="IntPtr.Zero"/>.
		/// </summary>
		/// <param name="ptr">Native pointer to a UTF8 NUL-terminated string allocated by libasound.</param>
		/// <param name="context">Context used for error messages if marshaling fails.</param>
		public static DiagnosticResult<string> PtrToStringUtf8Safe(IntPtr ptr, string context)
		{
			try
			{
				if (ptr == IntPtr.Zero)
				{
					return new DiagnosticResult<string>(true, string.Empty, null);
				}

				var s = Marshal.PtrToStringUTF8(ptr);
				if (s == null) return new DiagnosticResult<string>(true, string.Empty, null);
				return new DiagnosticResult<string>(true, s, null);
			}
			catch (Exception ex)
			{
				return new DiagnosticResult<string>(false, string.Empty, $"{context}: {ex}");
			}
		}
	}
}

