using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    public enum CurrentState
    {
        Unknown,
        ConfirmingTraining,
    }

    public class PromptStateDD
    {
        public PromptStateDD(DataDrivenResult result)
        {
            State = CurrentState.Unknown;
            Result = result;
        }

        public CurrentState State { get; set; }

        public DataDrivenResult Result { get; set; }
    }
}
