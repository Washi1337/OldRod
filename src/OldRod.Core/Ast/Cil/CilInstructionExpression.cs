using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using OldRod.Core.Architecture;

namespace OldRod.Core.Ast.Cil
{
    public class CilInstructionExpression : CilExpression
    {
        public CilInstructionExpression(CilOpCode opCode)
            : this(opCode, null, Enumerable.Empty<CilExpression>())
        {
        }
       
        public CilInstructionExpression(CilOpCode opCode, object operand)
            : this(opCode, operand, Enumerable.Empty<CilExpression>())
        {
        }

        public CilInstructionExpression(CilOpCode opCode, object operand, params CilExpression[] arguments)
            : this(opCode, operand, arguments.AsEnumerable())
        {
        }

        public CilInstructionExpression(CilOpCode opCode, object operand, IEnumerable<CilExpression> arguments)
        {
            OpCode = opCode;
            Operand = operand;
            Arguments = new AstNodeCollection<CilExpression>(this);
            
            foreach (var argument in arguments)
                Arguments.Add(argument);
        }

        public CilOpCode OpCode
        {
            get;
            set;
        }

        public object Operand
        {
            get;
            set;
        }

        public IList<CilExpression> Arguments
        {
            get;
        }

        public VMFlags AffectedFlags
        {
            get;
            set;
        }

        public bool ShouldEmitFlagsUpdate
        {
            get;
            set;
        }
        
        public bool InvertedFlagsUpdate
        {
            get;
            set;
        }

        public override void ReplaceNode(CilAstNode node, CilAstNode newNode)
        {
            AssertNodeParents(node, newNode);
            int index = Arguments.IndexOf((CilExpression) node);
            
            if (newNode == null)
                Arguments.RemoveAt(index);
            else
                Arguments[index] = (CilExpression) newNode;
        }

        public override IEnumerable<CilAstNode> GetChildren()
        {
            return Arguments;
        }

        public override void AcceptVisitor(ICilAstVisitor visitor)
        {
            visitor.VisitInstructionExpression(this);
        }

        public override TResult AcceptVisitor<TResult>(ICilAstVisitor<TResult> visitor)
        {
            return visitor.VisitInstructionExpression(this);
        }

        public override string ToString()
        {
            if (Operand == null)
                return $"{OpCode}({string.Join(", ", Arguments)})";
            if (Arguments.Count == 0)
                return OpCode + "(" + Operand + ")";
            return $"{OpCode}({Operand} : {string.Join(", ", Arguments)})";
        }
    }
}