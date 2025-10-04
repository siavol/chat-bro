using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace ChatBro.AiService.Plugins;

public class DateTimePlugin
{
    [KernelFunction("get_current_datetime")]
    [Description("Returns current local date and time.")]
    public DateTime CurrentDateTime() => DateTime.Now;
}