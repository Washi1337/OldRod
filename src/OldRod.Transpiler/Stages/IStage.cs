namespace OldRod.Transpiler.Stages
{
    public interface IStage
    {
        string Name { get; }

        void Run(DevirtualisationContext context);
    }
}