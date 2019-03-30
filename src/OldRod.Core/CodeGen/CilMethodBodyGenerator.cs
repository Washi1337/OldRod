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
using AsmResolver.Net.Cil;
using AsmResolver.Net.Cts;
using AsmResolver.Net.Signatures;
using OldRod.Core.Architecture;
using OldRod.Core.Ast.Cil;

namespace OldRod.Core.CodeGen
{
    public class CilMethodBodyGenerator
    {
        private readonly VMConstants _constants;
        private readonly TypeDefinition _flagHelperType;

        public CilMethodBodyGenerator(VMConstants constants, TypeDefinition flagHelperType)
        {
            _constants = constants ?? throw new ArgumentNullException(nameof(constants));
            _flagHelperType = flagHelperType ?? throw new ArgumentNullException(nameof(flagHelperType));
        }

        public CilMethodBody Compile(MethodDefinition method, CilCompilationUnit unit)
        {
            var methodBody = new CilMethodBody(method);
            
            var context = new CodeGenerationContext(methodBody, _constants, unit.FlagVariable.Signature, _flagHelperType);
            var visitor = new CilCodeGenerator(context);
            context.CodeGenerator = visitor;
            
            // Traverse and recompile the AST.
            methodBody.Instructions.AddRange(unit.AcceptVisitor(visitor));
            
            // Add variables to the method body.
            if (context.Variables.Count > 0)
            {
                methodBody.Signature = new StandAloneSignature(new LocalVariableSignature(context.Variables));
                methodBody.InitLocals = true;
            }
            
            methodBody.Instructions.OptimizeMacros();
            
            return methodBody;
        }
    }
}