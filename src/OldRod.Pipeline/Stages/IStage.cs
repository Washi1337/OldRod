namespace OldRod.Pipeline.Stages
{
    public interface IStage
    {
        string Name { get; }

        void Run(DevirtualisationContext context);
    }
}