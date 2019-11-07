#if ANDROID 
using Android.Util;
using System;

namespace Tracking.Services
{
    public class log4droid
    {

        public string Logger { get; set; }

        public void VerboseFormat(string fmt, params object[] args) { Log.Verbose(Logger, fmt, args); }
        public void DebugFormat(string fmt, params object[] args) { Log.Debug(Logger, fmt, args); }
        public void InfoFormat (string fmt, params object[] args) { Log.Info (Logger, fmt, args); }
        public void WarnFormat (string fmt, params object[] args) { Log.Warn (Logger, fmt, args); }
        public void ErrorFormat(string fmt, params object[] args) { Log.Error(Logger, fmt, args); }
        public void FatalFormat(string fmt, params object[] args) { Log.Error(Logger, fmt, args); }

        public void Verbose(string fmt, params object[] args) { Log.Verbose(Logger, fmt, args); }
        public void Debug(string fmt, params object[] args) { Log.Debug(Logger, fmt, args); }
        public void Info (string fmt, params object[] args) { Log.Info (Logger, fmt, args); }
        public void Warn (string fmt, params object[] args) { Log.Warn (Logger, fmt, args); }
        public void Error(string fmt, params object[] args) { Log.Error(Logger, fmt, args); }
        public void Fatal(string fmt, params object[] args) { Log.Error(Logger, fmt, args); }
    }
} 

#endif