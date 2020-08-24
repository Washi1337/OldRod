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
using AsmResolver.DotNet;
using OldRod.Core;
using OldRod.Core.Architecture;
using OldRod.Core.Recompiler;
using OldRod.Pipeline.Stages.OpCodeResolution;
using OldRod.Pipeline.Stages.VMMethodDetection;

namespace OldRod.Pipeline
{
    public class DevirtualisationContext : IVMFunctionResolver
    {
        public DevirtualisationContext(DevirtualisationOptions options, ModuleDefinition targetModule,
            ModuleDefinition runtimeModule, KoiStream koiStream, ILogger logger)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            TargetModule = targetModule ?? throw new ArgumentNullException(nameof(targetModule));
            RuntimeModule = runtimeModule ?? throw new ArgumentNullException(nameof(runtimeModule));
            KoiStream = koiStream ?? throw new ArgumentNullException(nameof(koiStream));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ReferenceImporter = new ReferenceImporter(targetModule);
        }

        public DevirtualisationOptions Options
        {
            get;
        }

        public ILogger Logger
        {
            get;
        }

        public ModuleDefinition TargetModule
        {
            get;
        }

        public ModuleDefinition RuntimeModule
        {
            get;
        }

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

        public MethodDefinition ResolveMethod(uint functionAddress)
        {
            // TODO: make use of dictionary instead of linear search.
            return VirtualisedMethods.FirstOrDefault(x => x.Function.EntrypointAddress == functionAddress)?.CallerMethod;
        }
    }
}