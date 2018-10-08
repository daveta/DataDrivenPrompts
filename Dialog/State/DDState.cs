using System.Collections.Generic;

namespace Microsoft.BotBuilderSamples
{
    public class DDState
    {
        public DDState()
        {
            Prompts = new List<string>();
            PromptIndex = 0;
        }

        public int PromptIndex;
        public List<string> Prompts { get; set; }
    }
}
