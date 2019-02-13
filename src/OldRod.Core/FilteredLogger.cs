namespace OldRod.Core
{
    public class FilteredLogger : ILogger
    {
        private ILogger _logger;

        public FilteredLogger(ILogger logger)
        {
            _logger = logger;
        }

        public bool IncludeDebug
        {
            get;
            set;
        } = true;

        public bool IncludeLog
        {
            get;
            set;
        } = true;

        public bool IncludeWarning
        {
            get;
            set;
        } = true;

        public bool IncludeError
        {
            get;
            set;
        } = true;
        
        public void Debug(string tag, string message)
        {
            if (IncludeDebug)
                _logger.Debug(tag, message);
        }

        public void Log(string tag, string message)
        {
            if (IncludeLog)
                _logger.Log(tag, message);
        }

        public void Warning(string tag, string message)
        {
            if (IncludeWarning)
                _logger.Warning(tag, message);
        }

        public void Error(string tag, string message)
        {
            if (IncludeError)
                _logger.Error(tag, message);
        }
    }
}