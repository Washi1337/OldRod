using System;
using System.Linq;
using AsmResolver;
using AsmResolver.Net.Cil;
using Carp.Core.Architecture;
using Carp.Core.Disassembly;

namespace Carp.Core.Stages.Transpiler
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