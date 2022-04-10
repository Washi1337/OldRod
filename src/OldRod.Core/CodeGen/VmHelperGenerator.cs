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
using AsmResolver.DotNet;
using AsmResolver.DotNet.Cloning;
using AsmResolver.PE.DotNet.Cil;
using OldRod.Core.Architecture;

namespace OldRod.Core.CodeGen
{
    public class VmHelperGenerator
    {
        private static readonly TypeDefinition VmHelperType;
        
        static VmHelperGenerator()
        {
            var module = ModuleDefinition.FromFile(typeof(VmHelper).Assembly.Location);
            VmHelperType = module.TopLevelTypes.First(x => x.Name == nameof(VmHelper));
        }
        
        public static TypeDefinition ImportFlagHelper(ModuleDefinition module, VMConstants constants)
        {
            // Clone flag helper class.
            var cloner = new MemberCloner(module, context => new UseExistingCorlibReferenceImporter(context));
            cloner.Include(VmHelperType);
            var result = cloner.Clone();
            var flagHelperType = result.ClonedMembers.OfType<TypeDefinition>().First();
            
            module.Assembly.Modules[0].TopLevelTypes.Add(flagHelperType);

            // Obtain static cctor.
            var constructor = flagHelperType.Methods.First(x => x.IsConstructor && x.IsStatic);
            var instructions = constructor.CilMethodBody.Instructions;
            instructions.Clear();

            // Assign values of flags to the fields.
            foreach (var entry in constants.Flags.OrderBy(x => x.Value))
            {
                instructions.Add(CilInstruction.CreateLdcI4(entry.Key));
                instructions.Add(new CilInstruction(CilOpCodes.Stsfld,
                    flagHelperType.Fields.First(x => x.Name == "FL_" + entry.Value.ToString())));
            }

            instructions.Add(new CilInstruction(CilOpCodes.Ret));

            return flagHelperType;
        }

        private sealed class UseExistingCorlibReferenceImporter : CloneContextAwareReferenceImporter
        {
            internal UseExistingCorlibReferenceImporter(MemberCloneContext context) : base(context) 
            {
            }

            protected override ITypeDefOrRef ImportType(TypeReference type)
            {
                var defAsm = type.Scope?.GetAssembly();
                if (defAsm is not null && defAsm.IsCorLib)
                    type = new TypeReference(Context.Module, TargetModule.CorLibTypeFactory.CorLibScope, type.Namespace, type.Name);
                return base.ImportType(type);
            }
        }
    }
}