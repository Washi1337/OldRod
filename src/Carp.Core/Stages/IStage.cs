namespace Carp.Core.Stages
{
    public interface IStage
    {
        string Name { get; }

        void Run(DevirtualisationContext context);
    }
}