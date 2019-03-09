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

using System.Collections.Generic;
using System.Linq;
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using OldRod.Core.Ast;
using OldRod.Core.Ast.Cil;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using Rivers;

namespace OldRod.Pipeline
{
  
    internal static class Utilities
    {
        public static Graph ConvertToGraphViz(this Graph graph, string nodeContentsProperty)
        {
            var newGraph = new Graph();
            foreach (var node in graph.Nodes)
            {
                var newNode = newGraph.Nodes.Add(node.Name);
                newNode.UserData["shape"] = "box3d";
                newNode.UserData["label"] = node.UserData[nodeContentsProperty];
            }

            foreach (var edge in graph.Edges)
            {
                var newEdge = newGraph.Edges.Add(edge.Source.Name, edge.Target.Name);
                if (edge.UserData.ContainsKey(ControlFlowGraph.ConditionProperty))
                    newEdge.UserData["label"] = edge.UserData[ControlFlowGraph.ConditionProperty];
            }

            return newGraph;
        }

        public static Graph ConvertToGraphViz(this IAstNode astNode, MethodDefinition method)
        {
            var formatter = new ShortAstFormatter(new CilInstructionFormatter(method.CilMethodBody));
            
            var graph = new Graph(false);
            var nodes = new Dictionary<IAstNode, Node>();
            var stack = new Stack<IAstNode>();
            stack.Push(astNode);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                string name;
                switch (current)
                {
                    case ILAstNode il:
                        name = il.AcceptVisitor(formatter);
                        break;
                    case CilAstNode cil:
                        name = cil.AcceptVisitor(formatter);
                        break;
                    default:
                        name = current.GetType().Name;
                        break;
                }

                var node = graph.Nodes.Add(nodes.Count.ToString());
                nodes.Add(current, node);
                node.UserData["shape"] = "box";
                node.UserData["label"] = nodes.Count + ": " + name;

                if (current.Parent != null && nodes.TryGetValue(current.Parent, out var parentNode))
                    parentNode.OutgoingEdges.Add(node);

                foreach (var child in current.GetChildren().Reverse())
                    stack.Push(child);
            }

            return graph;
        }

        private sealed class ShortAstFormatter : ICilAstVisitor<string>, IILAstVisitor<string>
        {
            private readonly CilInstructionFormatter _formatter;

            public ShortAstFormatter(CilInstructionFormatter formatter)
            {
                _formatter = formatter;
            }
            
            public string VisitCompilationUnit(CilCompilationUnit unit)
            {
                return "unit";
            }

            public string VisitBlock(CilAstBlock block)
            {
                return "block";
            }

            public string VisitExpressionStatement(CilExpressionStatement statement)
            {
                return "statement";
            }

            public string VisitInstructionExpression(CilInstructionExpression expression)
            {
                return string.Join(" - ", expression.Instructions.Select(i => i.Operand == null
                    ? _formatter.FormatOpCode(i.OpCode)
                    : _formatter.FormatOpCode(i.OpCode) + " " + _formatter.FormatOperand(i.OpCode.OperandType, i.Operand)));
            }

            public string VisitCompilationUnit(ILCompilationUnit unit)
            {
                return "unit";
            }

            public string VisitBlock(ILAstBlock block)
            {
                return "block";
            }

            public string VisitExpressionStatement(ILExpressionStatement statement)
            {
                return "statement";
            }

            public string VisitAssignmentStatement(ILAssignmentStatement statement)
            {
                return statement.Variable + " = ";
            }

            public string VisitInstructionExpression(ILInstructionExpression expression)
            {
                return expression.OpCode + " " + expression.Operand;
            }

            public string VisitVariableExpression(ILVariableExpression expression)
            {
                return expression.Variable.Name;
            }

            public string VisitVCallExpression(ILVCallExpression expression)
            {
                return expression.Metadata.ToString();
            }

            public string VisitPhiExpression(ILPhiExpression expression)
            {
                return "phi";
            }
        }
    }
}