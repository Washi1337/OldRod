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

namespace OldRod.Core.Ast.IL
{
    public interface IILAstVisitor
    {
        void VisitCompilationUnit(ILCompilationUnit unit);
        void VisitBlock(ILAstBlock block);
        void VisitExpressionStatement(ILExpressionStatement statement);
        void VisitAssignmentStatement(ILAssignmentStatement statement);
        void VisitInstructionExpression(ILInstructionExpression expression);
        void VisitVariableExpression(ILVariableExpression expression);
        void VisitVCallExpression(ILVCallExpression expression);
        void VisitPhiExpression(ILPhiExpression expression);
    }
    
    public interface IILAstVisitor<out TResult>
    {
        TResult VisitCompilationUnit(ILCompilationUnit unit);
        TResult VisitBlock(ILAstBlock block);
        TResult VisitExpressionStatement(ILExpressionStatement statement);
        TResult VisitAssignmentStatement(ILAssignmentStatement statement);
        TResult VisitInstructionExpression(ILInstructionExpression expression);
        TResult VisitVariableExpression(ILVariableExpression expression);
        TResult VisitVCallExpression(ILVCallExpression expression);
        TResult VisitPhiExpression(ILPhiExpression expression);
    }
}