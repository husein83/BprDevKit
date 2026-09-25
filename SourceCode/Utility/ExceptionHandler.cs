using System;
using BprDevKit.Utility;

namespace BprDevKit.Utility
{
    /// <summary>
    /// Exception handling utilities
    /// </summary>
    public static class ExceptionHandler
    {
        /// <summary>
        /// Handles an exception by logging it and optionally rethrowing
        /// </summary>
        public static void HandleException(Exception ex, LogLevel logLevel = LogLevel.Error, bool rethrow = true)
        {
            // In a real implementation, you would log the exception
            System.Console.WriteLine($"[{logLevel}] Exception: {ex.Message}");
            System.Console.WriteLine(ex.StackTrace);

            if (rethrow)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Executes an action with exception handling
        /// </summary>
        public static bool ExecuteWithExceptionHandling(Action action, LogLevel logLevel = LogLevel.Error)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                HandleException(ex, logLevel, false);
                return false;
            }
        }
    }
}
