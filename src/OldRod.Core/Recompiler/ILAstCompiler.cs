using System.Linq;
using AsmResolver;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast;

namespace OldRod.Core.Recompiler
{
    public class ILAstCompiler
    {
        private static readonly TypeDefinition FlagHelperType;
        
        static ILAstCompiler()
        {
            var assembly = WindowsAssembly.FromFile(typeof(FlagHelper).Assembly.Location);
            var image = assembly.NetDirectory.MetadataHeader.LockMetadata();
            FlagHelperType = image.Assembly.Modules[0].TopLevelTypes.First(x => x.Name == nameof(FlagHelper));
        }
        
        private readonly MetadataImage _image;
        private readonly VMConstants _constants;
        private TypeDefinition _flagHelperType;

        public ILAstCompiler(MetadataImage image, VMConstants constants)
        {
            _image = image;
            _constants = constants;
            SetupFlagHelper();
        }

        private void SetupFlagHelper()
        {
            // Clone flag helper class.
            var cloner = new MemberCloner(_image);
            _flagHelperType = cloner.CloneType(FlagHelperType);
            _image.Assembly.Modules[0].TopLevelTypes.Add(_flagHelperType);

            // Obtain static cctor.
            var constructor = _flagHelperType.Methods.First(x => x.IsConstructor && x.IsStatic);
            var instructions = constructor.CilMethodBody.Instructions;
            instructions.Clear();

            // Assign values of flags to the fields.
            foreach (var entry in _constants.Flags)
            {
                instructions.Add(CilInstruction.Create(CilOpCodes.Ldc_I4, entry.Key));
                instructions.Add(CilInstruction.Create(CilOpCodes.Stsfld,
                    _flagHelperType.Fields.First(x => x.Name == "FL_" + entry.Value.ToString())));
            }

            instructions.Add(CilInstruction.Create(CilOpCodes.Ret));
        }

        public CilMethodBody Compile(MethodDefinition method, ILCompilationUnit unit)
        {
            var context = new CompilerContext(_image, _constants, _flagHelperType);
            var visitor = new ILAstToCilVisitor(context);
            context.CodeGenerator = visitor;
            
            var methodBody = new CilMethodBody(method);

            // Traverse and recompile the AST.
            methodBody.Instructions.AddRange(unit.AcceptVisitor(visitor));
            
            // Add variables to the method body.
            if (context.Variables.Count > 0)
            {
                methodBody.Signature = new StandAloneSignature(new LocalVariableSignature(context.Variables.Values));
                methodBody.InitLocals = true;
            }
            
            return methodBody;
        }
    }
}