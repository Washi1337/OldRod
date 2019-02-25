using AsmResolver.Net.Cil;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using Rivers.Serialization.Dot;

namespace OldRod.Pipeline
{
    internal class BasicBlockSerializer : IUserDataSerializer
    {
        private readonly CilAstFormatter _formatter;
        private readonly DefaultUserDataSerializer _default = new DefaultUserDataSerializer();

        public BasicBlockSerializer()
        {
        }
        
        public BasicBlockSerializer(CilMethodBody methodBody)
        {
            _formatter = new CilAstFormatter(methodBody);
        }
        
        public string Serialize(string attributeName, object attributeValue)
        {
            switch (attributeValue)
            {
                case ILBasicBlock basicBlock:
                    return string.Join("\\l", basicBlock.Instructions) + "\\l";
                case ILAstBlock ilAstBlock:
                    return string.Join("\\l", ilAstBlock.Statements) + "\\l";
                case CilAstBlock cilAstBlock when _formatter != null:
                    return cilAstBlock.AcceptVisitor(_formatter);
                case CilAstBlock cilAstBlock:
                    return string.Join("\\l", cilAstBlock.Statements) + "\\l";
                default:
                    return _default.Serialize(attributeName, attributeValue);
            }
        }

        public object Deserialize(string attributeName, string rawValue)
        {
            return _default.Deserialize(attributeName, rawValue);
        }
    }
}