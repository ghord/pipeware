using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipeware.SourceImport
{
    public class SpectreCliLogger : ILogger
    {
        private object _lock = new object();
        private AsyncLocal<Scope?> _scope;
        private bool _verbose;

        public SpectreCliLogger(bool verbose)
        {
            _scope = new AsyncLocal<Scope?>();
            _verbose = verbose;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            if(_scope.Value is Scope parentScope)
            {
                _scope.Value = new Scope(parentScope, state.ToString());
            }
            else
            {
                _scope.Value = new Scope(this, state.ToString());
            }

            return _scope.Value;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (_verbose)
                return true;

            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            FormattedLogValuesHelper.TryMapParameters(ref state, (name, value) =>
            {
                if (value is SyntaxNode syntaxNode)
                {
                    return Markup.Escape(syntaxNode.ToString());
                }
                else if (value is IReadOnlyList<SyntaxNode> { } syntaxNodes)
                {
                    return syntaxNodes.ToString() is { } str ? Markup.Escape(str) : null;
                }
                return value;
            });

            if (_scope.Value is Scope scope)
            {
                _scope.Value.AddMessage(logLevel, state, exception, formatter);
            }
            else
            {
                LogMessage(logLevel, formatter(state, exception));
            }
        }

        private void LogMessage(LogLevel logLevel, string message)
        {
            lock (_lock)
            {
                var color = logLevel switch
                {
                    LogLevel.Trace => "gray",
                    LogLevel.Debug => "gray",
                    LogLevel.Information => "white",
                    LogLevel.Warning => "yellow",
                    LogLevel.Error => "red",
                    LogLevel.Critical => "red",
                    _ => throw new NotSupportedException(logLevel.ToString())
                };

                if(!_verbose && logLevel < LogLevel.Information)
                {
                    return;
                }
                
                AnsiConsole.MarkupLine($"[{color}]{message}[/]");
            }
        }

        class Scope : IDisposable
        {
            private SpectreCliLogger _logger;
            private Scope? _parentScope;
            private string? _message;
            private int _indent = 1;
            private List<(LogLevel logLevel, string message)> _messages = new();

            public Scope(SpectreCliLogger logger, string? message)
            {
                _logger = logger;
                _message = message;
            }

            public Scope(Scope parentScope, string? message)
            {
                _logger = parentScope._logger;
                _parentScope = parentScope;
                _message = message;
                _indent = parentScope._indent + 1;
            }

            public void AddMessage<TState>(LogLevel logLevel, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
               
                _messages.Add((logLevel, new string(' ', _indent * 4) + formatter(state, exception)));
            }

            public void Dispose()
            {
                _logger._scope.Value = _parentScope;

             
                if (_parentScope is not null)
                {
                    if (_message != null)
                    {
                        _parentScope.AddMessage(LogLevel.Information, _message, null, (s, e) => s!);
                    }

                    _parentScope._messages.AddRange(_messages);
                }
                else
                {
                    lock (_logger._lock)
                    {
                        if(_message != null)
                            _logger.LogMessage(LogLevel.Information, _message);

                        foreach (var (logLevel, message) in _messages)
                        {
                            _logger.LogMessage(logLevel, message);
                        }
                    }
                }

                _messages.Clear();
            }
        }

    }
}
