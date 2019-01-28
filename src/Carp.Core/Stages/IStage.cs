namespace Carp.Core
{
    public interface IStage
    {
        string Name { get; }

        void Run(DevirtualisationContext context);
    }
}