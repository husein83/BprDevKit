using System;
using BprDevKit.Utility.Logging;

namespace BprDevKit.Utility
{
    /// <summary>
    /// Exception handling utilities that forward directly to the central logging system
    /// </summary>
    public static class ExceptionHandler
    {
        /// <summary>
        /// Handles an exception by logging it and optionally rethrowing
        /// </summary>
        public static void HandleException(Exception ex, LogLevel logLevel = LogLevel.Error, bool rethrow = true, string source = "ExceptionHandler")
        {
            BprLogger.Instance.Log(logLevel, source, ex.Message, ex.StackTrace, exception: ex);

            if (rethrow)
            {
                throw ex; // Re-throw caller exception if explicitly required
            }
        }

        /// <summary>
        /// Executes an action with exception handling
        /// </summary>
        public static bool ExecuteWithExceptionHandling(Action action, LogLevel logLevel = LogLevel.Error, string source = "ExceptionHandler")
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, logLevel, false, source);
                return false;
            }
        }

        /// <summary>
        /// Executes an asynchronous action with exception handling
        /// </summary>
        public static async Task<bool> ExecuteWithExceptionHandlingAsync(Func<Task> action, LogLevel logLevel = LogLevel.Error, string source = "ExceptionHandler")
        {
            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, logLevel, false, source);
                return false;
            }
        }

        /// <summary>
        /// Safely executes a function returning a value with automated exception logging
        /// </summary>
        public static (bool Success, T? Result) TryExecute<T>(Func<T> func, LogLevel logLevel = LogLevel.Error, string source = "ExceptionHandler")
        {
            try
            {
                return (true, func());
            }
            catch (Exception ex)
            {
                HandleException(ex, logLevel, false, source);
                return (false, default);
            }
        }

        /// <summary>
        /// Safely executes an asynchronous function returning a value with automated exception logging
        /// </summary>
        public static async Task<(bool Success, T? Result)> TryExecuteAsync<T>(Func<Task<T>> func, LogLevel logLevel = LogLevel.Error, string source = "ExceptionHandler")
        {
            try
            {
                var result = await func();
                return (true, result);
            }
            catch (Exception ex)
            {
                HandleException(ex, logLevel, false, source);
                return (false, default);
            }
        }
    }
}
