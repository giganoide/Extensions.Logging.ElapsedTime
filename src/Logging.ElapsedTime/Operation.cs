using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Extensions.Logging.ElapsedTime
{
    internal enum CompletionBehaviour
    {
        Abandon,
        Complete,
        Silent
    }

    /// <summary>
    /// Records operation timings to the Serilog log.
    /// </summary>
    /// <remarks>
    /// Static members on this class are thread-safe. Instances
    /// of <see cref="Operation"/> are designed for use on a single thread only.
    /// </remarks>
    public class Operation : IDisposable
    {
        /// <summary>
        /// Property names attached to events by <see cref="Operation"/>s.
        /// </summary>
        public enum Properties
        {
            /// <summary>
            /// The timing, in milliseconds.
            /// </summary>
            Elapsed,

            /// <summary>
            /// Completion status, either <em>completed</em> or <em>discarded</em>.
            /// </summary>
            Outcome,

            /// <summary>
            /// A unique identifier added to the log context during
            /// the operation.
            /// </summary>
            OperationId
        };

        const string OutcomeCompleted = "completed", OutcomeAbandoned = "abandoned";

        ILogger _target;
        readonly string _messageTemplate;
        readonly object[] _args;
        readonly Stopwatch _stopwatch;

        IDisposable _popContext;
        CompletionBehaviour _completionBehaviour;
        readonly LogLevel _completionLevel;
        readonly LogLevel _abandonmentLevel;
        private Exception _exception;

        internal Operation(ILogger target, string messageTemplate, object[] args, CompletionBehaviour completionBehaviour, LogLevel completionLevel, LogLevel abandonmentLevel)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _messageTemplate = messageTemplate ?? throw new ArgumentNullException(nameof(messageTemplate));
            _args = args ?? throw new ArgumentNullException(nameof(args));
            _completionBehaviour = completionBehaviour;
            _completionLevel = completionLevel;
            _abandonmentLevel = abandonmentLevel;
            //_popContext = LogContext.PushProperty(nameof(Properties.OperationId), Guid.NewGuid());
            _stopwatch = Stopwatch.StartNew();
        }

        /// <summary>
        /// Returns the elapsed time of the operation. This will update during the operation, and be frozen once the
        /// operation is completed or canceled.
        /// </summary>
        public TimeSpan Elapsed => _stopwatch.Elapsed;
        

        /// <summary>
        /// Configure the logging levels used for completion and abandonment events.
        /// </summary>
        /// <param name="completion">The level of the event to write on operation completion.</param>
        /// <param name="abandonment">The level of the event to write on operation abandonment; if not
        /// specified, the <paramref name="completion"/> level will be used.</param>
        /// <returns>An object from which timings with the configured levels can be made.</returns>
        /// <remarks>If neither <paramref name="completion"/> nor <paramref name="abandonment"/> is enabled
        /// on the logger at the time of the call, a no-op result is returned.</remarks>
        //public static LevelledOperation At(LogLevel completion, LogLevel? abandonment = null)
        //{
        //    return Log.Logger.OperationAt(completion, abandonment);
        //}

        /// <summary>
        /// Complete the timed operation. This will write the event and elapsed time to the log.
        /// </summary>
        public void Complete()
        {
            _stopwatch.Stop();

            if (_completionBehaviour == CompletionBehaviour.Silent)
                return;

            Write(_target, _completionLevel, OutcomeCompleted);
        }
        
        /// <summary>
        /// Abandon the timed operation. This will write the event and elapsed time to the log.
        /// </summary>
        public void Abandon()
        {
            if (_completionBehaviour == CompletionBehaviour.Silent)
                return;

            Write(_target, _abandonmentLevel, OutcomeAbandoned);
        }

        /// <summary>
        /// Cancel the timed operation. After calling, no event will be recorded either through
        /// completion or disposal.
        /// </summary>
        public void Cancel()
        {
            _stopwatch.Stop();
            _completionBehaviour = CompletionBehaviour.Silent;
            PopLogContext();
        }

        /// <summary>
        /// Dispose the operation. If not already completed or canceled, an event will be written
        /// with timing information. Operations started with <see cref="Time"/> will be completed through
        /// disposal. Operations started with <see cref="Begin"/> will be recorded as abandoned.
        /// </summary>
        public void Dispose()
        {
            switch (_completionBehaviour)
            {
                case CompletionBehaviour.Silent:
                    break;

                case CompletionBehaviour.Abandon:
                    Write(_target, _abandonmentLevel, OutcomeAbandoned);
                    break;

                case CompletionBehaviour.Complete:
                    Write(_target, _completionLevel, OutcomeCompleted);
                    break;

                default:
                    throw new InvalidOperationException("Unknown underlying state value");
            }

            PopLogContext();
        }

        void PopLogContext()
        {
            _popContext?.Dispose();
            _popContext = null;
        }

        void Write(ILogger target, LogLevel level, string outcome)
        {
            _completionBehaviour = CompletionBehaviour.Silent;

            var elapsed = _stopwatch.Elapsed.TotalMilliseconds;

            target.Log(level, _exception, $"{_messageTemplate} {{{nameof(Properties.Outcome)}}} in {{{nameof(Properties.Elapsed)}:0.0}} ms", _args.Concat(new object[] { outcome, elapsed }).ToArray());

            PopLogContext();
        }

        /// <summary>
        /// Enriches resulting log event with the given exception.
        /// </summary>
        /// <param name="exception">Exception related to the event.</param>
        /// <returns>Same <see cref="Operation"/>.</returns>
        /// <seealso cref="LogEvent.Exception"/>
        public Operation SetException(Exception exception)
        {
            _exception = exception;
            return this;
        }
    }
}
