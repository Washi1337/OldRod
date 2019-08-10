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

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver;
using AsmResolver.Net.Cts;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.IL;
using OldRod.Core.Disassembly.ControlFlow;
using OldRod.Core.Recompiler;
using OldRod.Pipeline.Stages.OpCodeResolution;
using OldRod.Pipeline.Stages.VMMethodDetection;

namespace OldRod.Pipeline
{
    public class DevirtualisationContext : IVMFunctionResolver
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

        public bool AllVirtualisedMethodsRecompiled => VirtualisedMethods.All(x => x.CilCompilationUnit != null);

        public ICallableMemberReference ResolveMethod(uint functionAddress)
        {
            // TODO: make use of dictionary instead of linear search.
            return VirtualisedMethods.FirstOrDefault(x => x.Function.EntrypointAddress == functionAddress)?.CallerMethod;
        }
    }
}