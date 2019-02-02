using System;
using System.Linq;
using AsmResolver;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;
using OldRod.Core.Disassembly;

namespace OldRod.Core.Stages.Transpiler
{
    public class TranspilerStage : IStage
    {
        public const string Tag = "Transpiler";
        
        public string Name => "KoiVM IL transpiler stage";

        public void Run(DevirtualisationContext context)
        {

            
          
        }
    }
}