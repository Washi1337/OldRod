// Project OldRod - A KoiVM devirtualisation utility.
// Copyright (C) 2019 Washi
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.

using System.Linq;
using OldRod.Core.Ast.Cil;

namespace OldRod.Core.Recompiler.Transform
{
    public interface ICilAstTransform
    {
        void ApplyTransformation(RecompilerContext context, CilCompilationUnit unit);
    }
    public interface IChangeAwareCilAstTransform : ICilAstTransform
    {
        new bool ApplyTransformation(RecompilerContext context, CilCompilationUnit unit);
    }

    public abstract class ChangeAwareCilAstTransform : IChangeAwareCilAstTransform, ICilAstVisitor<bool>
    {
        public abstract string Name
        {
            get;
        }

        void ICilAstTransform.ApplyTransformation(RecompilerContext context, CilCompilationUnit unit)
        {
            ApplyTransformation(context, unit);
        }

        public virtual bool ApplyTransformation(RecompilerContext context, CilCompilationUnit unit)
        {
            bool changed = false;
            while (unit.AcceptVisitor(this))
            {
                changed = true;
                // Repeat until no more changes.
            }

            return changed;
        }

        public virtual bool VisitCompilationUnit(CilCompilationUnit unit)
        {
            bool changed = false;
            foreach (var node in unit.ControlFlowGraph.Nodes)
            {
                var block = (CilAstBlock) node.UserData[CilAstBlock.AstBlockProperty];
                changed |= block.AcceptVisitor(this);
            }

            return changed;
        }

        public virtual bool VisitBlock(CilAstBlock block)
        {
            bool changed = false;
            foreach (var statement in block.Statements)
                changed |= statement.AcceptVisitor(this);
            return changed;
        }

        public virtual bool VisitExpressionStatement(CilExpressionStatement statement)
        {
            return statement.Expression.AcceptVisitor(this);
        }

        public virtual bool VisitAssignmentStatement(CilAssignmentStatement statement)
        {
            return statement.Value.AcceptVisitor(this);
        }

        public virtual bool VisitInstructionExpression(CilInstructionExpression expression)
        {
            bool changed = false;
            foreach (var argument in expression.Arguments.ToArray())
                changed |= argument.AcceptVisitor(this);
            return changed;
        }

        public virtual bool VisitUnboxToVmExpression(CilUnboxToVmExpression expression)
        {
            return expression.Expression.AcceptVisitor(this);
        }

        public virtual bool VisitVariableExpression(CilVariableExpression expression)
        {
            return false;
        }


    }
}