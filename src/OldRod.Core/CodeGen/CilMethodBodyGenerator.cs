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
using System.Linq;
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

        public bool EnableStackVerification
        {
            get;
            set;
        } = true;

        public bool EnableExceptionHandlerValidation
        {
            get;
            set;
        } = true;

        public CilMethodBody Compile(MethodDefinition method, CilCompilationUnit unit)
        {
            var methodBody = new CilMethodBody(method);
            
            var context = new CodeGenerationContext(methodBody, _constants, unit.FlagVariable, _flagHelperType);
            
            var cilGenerator = new CilCodeGenerator(context);
            context.CodeGenerator = cilGenerator;
            
            // Traverse and recompile the AST.
            methodBody.Instructions.AddRange(unit.AcceptVisitor(cilGenerator));
            
            // Add variables to the method body.
            if (context.Variables.Count > 0)
            {
                methodBody.Signature = new StandAloneSignature(new LocalVariableSignature(context.Variables.Values));
                methodBody.InitLocals = true;
            }

            methodBody.Instructions.OptimizeMacros();
            
            // Add all generated exception handlers to the method body.
            var handlers = context.ExceptionHandlers.Values.ToList();
            handlers.Sort(new EHComparer());
            foreach (var handler in handlers)
            {
                if (EnableExceptionHandlerValidation)
                    AssertValidityExceptionHandler(method, handler);
                methodBody.ExceptionHandlers.Add(handler);
            }

            if (!EnableStackVerification)
            {
                methodBody.ComputeMaxStackOnBuild = false;
                methodBody.MaxStack = ushort.MaxValue;
            }
            
            return methodBody;
        }

        private static void AssertValidityExceptionHandler(MethodDefinition method, ExceptionHandler handler)
        {
            if (handler.TryStart == null
                || handler.TryEnd == null
                || handler.HandlerStart == null
                || handler.HandlerEnd == null)
            {
                throw new CilCodeGeneratorException(
                    $"Detected an incomplete exception handler in the generated method body of {method}. "
                    + $"This could be a bug in the code generator.",
                    new NullReferenceException("One or more of the EH boundaries was set to null."));
            }

            switch (handler.HandlerType)
            {
                case ExceptionHandlerType.Exception:
                    if (handler.CatchType == null)
                    {
                        throw new CilCodeGeneratorException(
                            $"Detected an incomplete exception handler in the generated method body of {method}. "
                            + $"This could be a bug in the code generator.",
                            new NullReferenceException("Expected an exception type in a try-catch construct."));
                    }
                    break;
                
                case ExceptionHandlerType.Filter:
                    if (handler.FilterStart == null)
                    {
                        throw new CilCodeGeneratorException(
                            $"Detected an incomplete exception handler in the generated method body of {method}. "
                            + $"This could be a bug in the code generator.",
                            new NullReferenceException("Expected a filter start in a try-filter construct."));
                    }
                    break;
                
                case ExceptionHandlerType.Finally:
                case ExceptionHandlerType.Fault:
                    if (handler.CatchType != null || handler.FilterStart != null)
                    {
                        throw new CilCodeGeneratorException(
                            $"Detected an exception handler with too many parameters in the generated method body of {method}. "
                            + $"This could be a bug in the code generator.");
                    }
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

    }
}