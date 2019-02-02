namespace OldRod.Transpiler
{
    public interface ILogger
    {
        void Debug(string tag, string message);
        
        void Log(string tag, string message);

        void Warning(string tag, string message);
        
        void Error(string tag, string message);
    }
}