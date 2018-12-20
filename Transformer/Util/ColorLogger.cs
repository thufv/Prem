using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Prem.Util
{
    /// <summary>
    /// A simple logger with color support. Messages are printed to stdout.
    /// This class is implemented as a thread-safe singleton.
    /// Call property <code>Instance</code> to obtain the instance.
    /// </summary>
    public sealed class ColorLogger
    {
        /// <summary>
        /// The internal instance, shall be singleton.
        /// </summary>
        private static ColorLogger instance = null;
        
        /// <summary>
        /// The lock.
        /// </summary>
        private static readonly object padlock = new object();

        /// <summary>
        /// Disable constructor.
        /// </summary>
        private ColorLogger() {}

        /// <summary>
        /// The instance, read only for callers.
        /// </summary>
        /// <value>The logger instance.</value>
        public static ColorLogger Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null) {
                        instance = new ColorLogger();
                    }
                    return instance;
                }
            }
        }

        /// <summary>
        /// Get string representation for each log level.
        /// </summary>
        /// <returns>The string representation.</returns>
        private string LevelString(LogLevel level)
        {
            switch (level) {
                case LogLevel.ERROR: return "Error";
                case LogLevel.WARNING: return "Warning";
                case LogLevel.FAILURE: return "Failure";
                case LogLevel.SUCCESS: return "Success";
                case LogLevel.INFO: return "Info";
                case LogLevel.DEBUG: return "Debug";
                case LogLevel.FINE: return "Fine";
            }
            return "";
        }

        /// <summary>
        /// Get color for each log level.
        /// </summary>
        /// <returns>The color.</returns>
        private Color LevelColor(LogLevel level)
        {
            switch (level) {
                case LogLevel.ERROR: return Color.Red;
                case LogLevel.WARNING: return Color.Yellow;
                case LogLevel.FAILURE: return Color.DarkRed;
                case LogLevel.SUCCESS: return Color.DarkGreen;
                case LogLevel.INFO: return Color.Cyan;
                case LogLevel.DEBUG: return Color.LightBlue;
                case LogLevel.FINE: return Color.White;
            }
            return Color.White;
        }
        
        /// <summary>
        /// Set the lowest log level that shall be displayed.
        /// </summary>
        /// <value>The lowest log level. Default<code>INFO</code>.</value>
        public LogLevel DisplayLevel { get; set; } = LogLevel.INFO;

        /// <summary>
        /// Whether to enable colorful messages.
        /// </summary>
        /// <value>Enable/disable. Default disable.</value>
        public bool ShowColor { get; set; }

        public string ExplicitlyToString(object obj)
        {
            if (obj is IEnumerable && !(obj is IDictionary) && !(obj is String))
            {
                var o = (IEnumerable)obj;
                var sb = new StringBuilder();
                sb.Append("{ ");
                var pointer = o.GetEnumerator();
                if (pointer.MoveNext())
                {
                    sb.Append(ExplicitlyToString(pointer.Current));
                }
                while (pointer.MoveNext())
                {
                    sb.Append(", ");
                    sb.Append(ExplicitlyToString(pointer.Current));
                }
                sb.Append(" }");
                return sb.ToString();
            }

            return obj.ToString();
        }

        public string ExplicitlyFormat(string format, params object[] args)
        {
            var ss = new string[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                ss[i] = ExplicitlyToString(args[i]);
            }
            return String.Format(format, ss);
        }

        /// <summary>
        /// Display message.
        /// Format: <code>[level] msg</code>.
        /// </summary>
        /// <param name="Level">The log level.</param>
        /// <param name="text">The plain text.</param>
        public void Raw(LogLevel level, string text)
        {
            // check priority
            if (level > DisplayLevel) return;

            // display text
            if (ShowColor)
            {
                Colorful.Console.WriteLine(text, LevelColor(level));
            }
            else
            {
                Console.WriteLine(text);
            }
        }

        public void Raw(LogLevel level, string format, params object[] args)
        {
            Raw(level, ExplicitlyFormat(format, args));
        }

        public void Raw(string text)
        {
            Raw(LogLevel.FINE, text);
        }

        public void Raw(string format, params object[] args)
        {
            Raw(LogLevel.FINE, format, args);
        }

        /// <summary>
        /// Display message.
        /// Format: <code>[level] msg</code>.
        /// </summary>
        /// <param name="Level">The log level.</param>
        /// <param name="msg">The log message.</param>
        public void Log(LogLevel level, string msg)
        {
            var fullMessage = "[" + LevelString(level) + "] " + msg;
            Raw(level, fullMessage);
        }

        /// <summary>
        /// Display message with variable arguments supported.
        /// Equivalent to <code>Log(level, String.Format(format, args))</code>.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Log(LogLevel level, string format, params object[] args)
        {
            var msg = String.Format(format, args);
            var fullMessage = "[" + LevelString(level) + "] " + msg;
            Raw(level, fullMessage);
        }

        /// <summary>
        /// Display <code>ERROR</code> messages. 
        /// A Simpler call of <code>Log(LogLevel.ERROR, msg)</code>.
        /// </summary>
        public void Error(string msg)
        {
            Log(LogLevel.ERROR, msg);
        }

        /// <summary>
        /// Display <code>ERROR</code> messages, with variable arguments supported.
        /// A Simpler call of <code>Log(LogLevel.ERROR, format, args)</code>.
        /// </summary>
        public void Error(string format, params object[] args)
        {
            Log(LogLevel.ERROR, format, args);
        }

        /// <summary>
        /// Display <code>WARNING</code> messages. 
        /// A Simpler call of <code>Log(LogLevel.WARNING, msg)</code>.
        /// </summary>
        public void Warning(string msg)
        {
            Log(LogLevel.WARNING, msg);
        }

        /// <summary>
        /// Display <code>WARNING</code> messages, with variable arguments supported.
        /// A Simpler call of <code>Log(LogLevel.WARNING, format, args)</code>.
        /// </summary>
        public void Warning(string format, params object[] args)
        {
            Log(LogLevel.WARNING, format, args);
        }

        /// <summary>
        /// Display <code>FAILURE</code> messages. 
        /// A Simpler call of <code>Log(LogLevel.FAILURE, msg)</code>.
        /// </summary>
        public void Failure(string msg)
        {
            Log(LogLevel.FAILURE, msg);
        }

        /// <summary>
        /// Display <code>FAILURE</code> messages, with variable arguments supported.
        /// A Simpler call of <code>Log(LogLevel.FAILURE, format, args)</code>.
        /// </summary>
        public void Failure(string format, params object[] args)
        {
            Log(LogLevel.FAILURE, format, args);
        }

        /// <summary>
        /// Display <code>SUCCESS</code> messages. 
        /// A Simpler call of <code>Log(LogLevel.SUCCESS, msg)</code>.
        /// </summary>
        public void Success(string msg)
        {
            Log(LogLevel.SUCCESS, msg);
        }

        /// <summary>
        /// Display <code>SUCCESS</code> messages, with variable arguments supported.
        /// A Simpler call of <code>Log(LogLevel.SUCCESS, format, args)</code>.
        /// </summary>
        public void Success(string format, params object[] args)
        {
            Log(LogLevel.SUCCESS, format, args);
        }

        /// <summary>
        /// Display <code>INFO</code> messages. 
        /// A Simpler call of <code>Log(LogLevel.INFO, msg)</code>.
        /// </summary>
        public void Info(string msg)
        {
            Log(LogLevel.INFO, msg);
        }

        /// <summary>
        /// Display <code>INFO</code> messages, with variable arguments supported.
        /// A Simpler call of <code>Log(LogLevel.INFO, format, args)</code>.
        /// </summary>
        public void Info(string format, params object[] args)
        {
            Log(LogLevel.INFO, format, args);
        }

        /// <summary>
        /// Display <code>DEBUG</code> messages. 
        /// A Simpler call of <code>Log(LogLevel.DEBUG, msg)</code>.
        /// </summary>
        public void Debug(string msg)
        {
            Log(LogLevel.DEBUG, msg);
        }

        /// <summary>
        /// Display <code>DEBUG</code> messages, with variable arguments supported.
        /// A Simpler call of <code>Log(LogLevel.DEBUG, format, args)</code>.
        /// </summary>
        public void Debug(string format, params object[] args)
        {
            Log(LogLevel.DEBUG, format, args);
        }

        /// <summary>
        /// Display <code>FINE</code> messages. 
        /// A Simpler call of <code>Log(LogLevel.FINE, msg)</code>.
        /// </summary>
        public void Fine(string msg)
        {
            Log(LogLevel.FINE, msg);
        }

        /// <summary>
        /// Display <code>FINE</code> messages, with variable arguments supported.
        /// A Simpler call of <code>Log(LogLevel.FINE, format, args)</code>.
        /// </summary>
        public void Fine(string format, params object[] args)
        {
            Log(LogLevel.FINE, format, args);
        }

        private int _identLevel = 0;

        private int _width = 4;

        public void IncIndent()
        {
            _identLevel++;
        }

        public void DecIndent()
        {
            System.Diagnostics.Debug.Assert(_identLevel > 0, "Cannot decrease indent.");
            _identLevel--;
        }

        public void Tree(LogLevel level, string msg)
        {
            var whiteSpaces = new String(' ', _width * _identLevel);
            Raw(level, whiteSpaces + msg);
        }

        public void Tree(LogLevel level, string format, params object[] args)
        {
            Tree(level, ExplicitlyFormat(format, args));
        }

        public void Tree(string text)
        {
            Tree(LogLevel.FINE, text);
        }

        public void Tree(string format, params object[] args)
        {
            Tree(LogLevel.FINE, format, args);
        }

        /// <summary>
        /// Display <code>DEBUG</code> messages. 
        /// A Simpler call of <code>Log(LogLevel.DEBUG, msg)</code>.
        /// </summary>
        public void DebugRaw(string msg)
        {
            Raw(LogLevel.DEBUG, msg);
        }

        /// <summary>
        /// Display <code>DEBUG</code> messages, with variable arguments supported.
        /// A Simpler call of <code>Log(LogLevel.DEBUG, format, args)</code>.
        /// </summary>
        public void DebugRaw(string format, params object[] args)
        {
            DebugRaw(String.Format(format, args));
        }

        /// <summary>
        /// Display a raw <code>FINE</code> messages. 
        /// A Simpler call of <code>Log(LogLevel.FINE, msg)</code>.
        /// </summary>
        public void FineRaw(string msg)
        {
            Raw(LogLevel.FINE, msg);
        }

        /// <summary>
        /// Display <code>FINE</code> messages, with variable arguments supported.
        /// A Simpler call of <code>Log(LogLevel.FINE, format, args)</code>.
        /// </summary>
        public void FineRaw(string format, params object[] args)
        {
            FineRaw(String.Format(format, args));
        }

        /// <summary>
        /// Check if the specified <code>level</code> is loggable in the current log level.
        /// </summary>
        public bool IsLoggable(LogLevel level)
        {
            return level <= DisplayLevel;
        }
    }

    /// <summary>
    /// Log levels. The priority is from the highest to the lowest.
    /// </summary>
    public enum LogLevel { ERROR, WARNING, FAILURE, SUCCESS, INFO, DEBUG, FINE }
}