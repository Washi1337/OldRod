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

using OldRod.Core.Architecture;

namespace OldRod.Pipeline.Stages.KoiStreamParsing
{
    public class KoiStreamParserStage : IStage
    {
        private const string Tag = "#KoiParser";
        
        public string Name => "#Koi stream parsing stage";
        
        public void Run(DevirtualisationContext context)
        {
            context.Logger.Debug(Tag, "Parsing #Koi stream...");
            context.KoiStream = context.TargetModule.Header.GetStream<KoiStream>();

            if (context.KoiStream == null)
            {
                throw new DevirtualisationException(
                    "Koi stream was not found in the target PE. This could be because the input file is " +
                    "not protected with KoiVM, or the metadata stream uses a name that is different " +
                    "from the one specified in the input parameters.");
            }
        }
        
    }
}