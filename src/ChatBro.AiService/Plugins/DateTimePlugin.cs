using System.ComponentModel;

namespace ChatBro.AiService.Plugins;

public static class DateTimePlugin
{
    [Description("Returns current local date and time.")]
    public static DateTime CurrentDateTime() => DateTime.Now;
}