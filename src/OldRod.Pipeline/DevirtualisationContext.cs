using System;
using System.Collections.Generic;
using AsmResolver;
using AsmResolver.Net.Cts;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Pipeline.Stages.OpCodeResolution;
using OldRod.Pipeline.Stages.VMEntryDetection;

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

            ReferenceImporter = new ReferenceImporter(targetImage);
        }

        public DevirtualisationOptions Options
        {
            get;
        }

        public ILogger Logger
        {
            get;
        }

        public MetadataImage TargetImage
        {
            get;
        }

        public WindowsAssembly TargetAssembly => TargetImage.Header.NetDirectory.Assembly;

        public MetadataImage RuntimeImage
        {
            get;
        }

        public WindowsAssembly RuntimeAssembly => RuntimeImage.Header.NetDirectory.Assembly;

        public ReferenceImporter ReferenceImporter
        {
            get;
        }

        public KoiStream KoiStream
        {
            get;
            set;
        }

        public VMEntryInfo VMEntryInfo
        {
            get;
            set;
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

        public ICollection<VirtualisedMethod> VirtualisedMethods
        {
            get;
        } = new List<VirtualisedMethod>();
    }
}