﻿using System;
using JetBrains.Annotations;

namespace BigWatson.Shared.Misc
{
    /// <summary>
    /// A struct that wraps the result of a safe async operation
    /// </summary>
    /// <typeparam name="T">The expected return type</typeparam>
    public struct AsyncOperationResult<T> : IEquatable<AsyncOperationResult<T>>, IEquatable<T> where T : class
    {
        /// <summary>
        /// Gets the available result
        /// </summary>
        [CanBeNull]
        public T Result { get; }

        /// <summary>
        /// Gets the result status
        /// </summary>
        public AsyncOperationStatus Status { get; }

        // Private constructor
        private AsyncOperationResult([CanBeNull] T result, AsyncOperationStatus status)
        {
            Result = result;
            Status = status;
        }

        /// <summary>
        /// Calls the default Equals method for the inner result of the wrapped instance
        /// </summary>
        /// <param name="other">The other instance to compare</param>
        public bool Equals(AsyncOperationResult<T> other) => Equals(other.Result);

        /// <summary>
        /// Calls the default Equals method for another value of the same type as the result of this instance
        /// </summary>
        /// <param name="other">The other value to compare</param>
        public bool Equals([CanBeNull] T other) => Result == null && other == null || Result?.Equals(other) == true;

        // Implicit cast for the inner result
        public static implicit operator T(AsyncOperationResult<T> wrappedResult) => wrappedResult.Result;

        // Implicit cast to check if the operation completed successfully
        public static implicit operator bool(AsyncOperationResult<T> wrappedResult) => wrappedResult.Status == AsyncOperationStatus.RunToCompletion;

        // Implicit converter for successful results
        public static implicit operator AsyncOperationResult<T>([CanBeNull] T result)
        {
            return new AsyncOperationResult<T>(result, AsyncOperationStatus.RunToCompletion);
        }

        // Implicit converters for faulted results
        public static implicit operator AsyncOperationResult<T>(AsyncOperationStatus status)
        {
            if (status == AsyncOperationStatus.RunToCompletion) throw new InvalidCastException("This implicit operator is not valid in this case");
            return new AsyncOperationResult<T>(null, status);
        }
    }

    /// <summary>
    /// Indicates the result status of a self-contained async operation
    /// </summary>
    public enum AsyncOperationStatus
    {
        /// <summary>
        /// The operation was completed successfully and the result is valid
        /// </summary>
        RunToCompletion,

        /// <summary>
        /// The operation was canceled and no return value is available
        /// </summary>
        Canceled,

        /// <summary>
        /// An invalid workflow was detected and the operation was stopped
        /// </summary>
        InternallyAborted,

        /// <summary>
        /// An exception was generated and handled within the async method
        /// </summary>
        Faulted
    }
}