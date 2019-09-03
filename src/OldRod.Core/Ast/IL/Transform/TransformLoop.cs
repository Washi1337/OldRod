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

namespace OldRod.Core.Ast.IL.Transform
{
    public class TransformLoop : IChangeAwareILAstTransform
    {
        public event EventHandler IterationStart;
        public event EventHandler IterationEnd;
        public event EventHandler<ILTransformEventArgs> TransformStart;
        public event EventHandler<ILTransformEventArgs> TransformEnd;
        
        public TransformLoop(string name, int maxIterations, IEnumerable<IChangeAwareILAstTransform> transforms)
        {
            Name = name;
            MaxIterations = maxIterations;
            Transforms = new List<IChangeAwareILAstTransform>(transforms);
        }
        
        public string Name
        {
            get;
        }

        public IList<IChangeAwareILAstTransform> Transforms
        {
            get;
        }

        public int MaxIterations
        {
            get;
        }

        public bool ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            int iteration = 0;
            
            bool changed = true;
            while (changed && iteration < MaxIterations)
            {
                iteration++;
                logger.Debug2(Name, $"Started iteration {iteration}...");
                OnIterationStart();
                
                changed = PerformSingleIteration(unit, logger, iteration);
                
                logger.Debug2(Name, $"Finished iteration {iteration} (AST has changed: {changed}).");
                OnIterationEnd();
            }

            if (iteration == MaxIterations && changed)
            {
                logger.Warning(Name,
                    $"Reached maximum amount of iterations of {MaxIterations} and AST is "
                    + "still changing. This might be a bug in the transformer pipeline where transforms keep "
                    + "cancelling each other out, or the method to devirtualise is too complex for the provided "
                    + "upper bound of iterations.");
            }

            return iteration > 1;
        }

        private bool PerformSingleIteration(ILCompilationUnit unit, ILogger logger, int iterationNumber)
        {
            bool changed = false;
            foreach (var transform in Transforms)
            {
                logger.Debug2(Name, "Applying " + transform.Name + "...");
                OnTransformStart(new ILTransformEventArgs(unit, transform, iterationNumber));
                changed |= transform.ApplyTransformation(unit, logger);
                OnTransformEnd(new ILTransformEventArgs(unit, transform, iterationNumber));
            }

            return changed;
        }

        void IILAstTransform.ApplyTransformation(ILCompilationUnit unit, ILogger logger)
        {
            ApplyTransformation(unit, logger);
        }

        protected virtual void OnTransformStart(ILTransformEventArgs e)
        {
            TransformStart?.Invoke(this, e);
        }

        protected virtual void OnTransformEnd(ILTransformEventArgs e)
        {
            TransformEnd?.Invoke(this, e);
        }

        protected virtual void OnIterationStart()
        {
            IterationStart?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnIterationEnd()
        {
            IterationEnd?.Invoke(this, EventArgs.Empty);
        }
    }
}