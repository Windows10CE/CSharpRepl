﻿
using System.Threading.Tasks;

namespace CSDiscordService.Eval
{
    public interface IPreProcessorService
    {
        Task PreProcess(ScriptExecutionContext context);
    }
}
