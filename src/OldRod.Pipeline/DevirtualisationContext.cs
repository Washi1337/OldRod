using System;
using System.Collections.Generic;
using AsmResolver.Net.Cts;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Pipeline.Stages.OpCodeResolution;

namespace OldRod.Pipeline
{
    public class DevirtualisationContext
    {
        public DevirtualisationContext(DevirtualisationOptions options, MetadataImage targetImage, MetadataImage runtimeImage, ILogger logger)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            TargetImage = targetImage ?? throw new ArgumentNullException(nameof(targetImage));
            RuntimeImage = runtimeImage ?? throw new ArgumentNullException(nameof(runtimeImage));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public DevirtualisationOptions Options
        {
            get;
        }

        public MetadataImage TargetImage
        {
            get;
        }

        public MetadataImage RuntimeImage
        {
            get;
        }

        public ILogger Logger
        {
            get;
        }

        public VMConstants Constants
        {
            get;
            set;
        }
        
        public OpCodeMapping OpCodeMapping
        {
            get;
            set;
        }

        public KoiStream KoiStream
        {
            get;
            set;
        }

        public IDictionary<VMExportInfo, ControlFlowGraph> ControlFlowGraphs
        {
            get;
            set;
        } = new Dictionary<VMExportInfo, ControlFlowGraph>();

        public IDictionary<VMExportInfo, ILCompilationUnit> CompilationUnits
        {
            get;
            set;
        } = new Dictionary<VMExportInfo, ILCompilationUnit>();
    }
}