using OldRod.Core.Disassembly.ControlFlow;
using Rivers;

namespace OldRod.Pipeline
{
    internal static class Utilities
    {
        public static Graph ConvertToGraphViz(Graph graph, string nodeContentsProperty)
        {
            var newGraph = new Graph();
            newGraph.UserData["rankdir"] = "LR";
            
            foreach (var node in graph.Nodes)
            {
                var newNode = newGraph.Nodes.Add(node.Name);
                newNode.UserData["shape"] = "record";
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
        
    }
}