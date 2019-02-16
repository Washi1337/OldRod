using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;

namespace OldRod.Pipeline
{
    public class VirtualisedMethod
    {
        public VirtualisedMethod(uint exportId, VMExportInfo exportInfo)
        {
            ExportId = exportId;
            ExportInfo = exportInfo;
        }

        public uint ExportId
        {
            get;
            set;
        }

        public VMExportInfo ExportInfo
        {
            get;
            set;
        }

        public MethodSignature ConvertedMethodSignature
        {
            get;
            set;
        }
        
        public MethodDefinition CallerMethod
        {
            get;
            set;
        }

        public ControlFlowGraph ControlFlowGraph
        {
            get;
            set;
        }

        public ILCompilationUnit ILCompilationUnit
        {
            get;
            set;
        }

        public CilCompilationUnit CilCompilationUnit
        {
            get;
            set;
        }
        
    }
}