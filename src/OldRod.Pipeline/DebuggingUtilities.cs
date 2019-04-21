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
  
    internal static class DebuggingUtilities
    {
        public static Graph ConvertToGraphViz(this Graph graph, string nodeContentsProperty)
        {
            var newGraph = new Graph();

            var clusters = new Dictionary<SubGraph, SubGraph>();
            foreach (var subGraph in graph.SubGraphs)
            {
                var newSubGraph = new SubGraph(newGraph, "cluster_" + clusters.Count);
                newSubGraph.UserData["color"] = "red";
                newGraph.SubGraphs.Add(newSubGraph);
                clusters[subGraph] = newSubGraph;
            }

            foreach (var node in graph.Nodes)
            {
                var newNode = newGraph.Nodes.Add(node.Name);
                newNode.UserData["shape"] = "box3d";

                if (node.UserData.TryGetValue(nodeContentsProperty, out var contents))
                {
                    newNode.UserData["label"] = contents;
                }
                else
                {
                    newNode.UserData["color"] = "red";
                    newNode.UserData["label"] = "?";
                }

                foreach (var subGraph in node.SubGraphs)
                    newNode.SubGraphs.Add(clusters[subGraph]);
            }

            foreach (var edge in graph.Edges)
            {
                var newEdge = newGraph.Edges.Add(edge.Source.Name, edge.Target.Name);
                if (edge.UserData.ContainsKey(ControlFlowGraph.ConditionProperty))
                {
                    if (edge.UserData[ControlFlowGraph.ConditionProperty] is int x)
                    {
                        switch (x)
                        {
                            case -1:
                            case -2:
                                newEdge.UserData["color"] = "grey";
                                newEdge.UserData["style"] = "dashed";
                                break;
                            
                            case 0:
                                newEdge.UserData["color"] = "red";
                                break;
                        }
                    }

                    newEdge.UserData["label"] = edge.UserData[ControlFlowGraph.ConditionProperty];
                }
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
            
            public string VisitCompilationUnit(CilCompilationUnit unit) => "unit";
            public string VisitBlock(CilAstBlock block) => "block";
            public string VisitExpressionStatement(CilExpressionStatement statement) => "statement";
            public string VisitAssignmentStatement(CilAssignmentStatement statement) => $"stloc {statement.Variable.Name}";

            public string VisitInstructionExpression(CilInstructionExpression expression)
            {
                return string.Join(" - ", expression.Instructions.Select(i => i.Operand == null
                    ? _formatter.FormatOpCode(i.OpCode)
                    : $"{_formatter.FormatOpCode(i.OpCode)} {_formatter.FormatOperand(i.OpCode.OperandType, i.Operand)}"));
            }

            public string VisitUnboxToVmExpression(CilUnboxToVmExpression expression) => $"unbox.tovm {expression.Type}";
            public string VisitVariableExpression(CilVariableExpression expression) => expression.Variable.Name;

            public string VisitCompilationUnit(ILCompilationUnit unit) => "unit";
            public string VisitBlock(ILAstBlock block) => "block";
            public string VisitExpressionStatement(ILExpressionStatement statement) => "statement";
            public string VisitAssignmentStatement(ILAssignmentStatement statement) => statement.Variable + " = ";
            public string VisitInstructionExpression(ILInstructionExpression expression) => $"{expression.OpCode} {expression.Operand}";
            public string VisitVariableExpression(ILVariableExpression expression) => expression.Variable.Name;
            public string VisitVCallExpression(ILVCallExpression expression) => expression.Annotation.ToString();
            public string VisitPhiExpression(ILPhiExpression expression) => "phi";
            
        }
    }
}