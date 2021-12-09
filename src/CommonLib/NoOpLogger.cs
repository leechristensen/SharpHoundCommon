﻿using System;
using Microsoft.Extensions.Logging;

namespace SharpHoundCommonLib
{
    public class NoOpLogger : ILogger
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return default;
        }
    }
}