using System.ComponentModel;

namespace ChatBro.Server.Services.AI.Plugins;

public static class DateTimePlugin
{
    [Description("Returns current local date and time.")]
    public static DateTime CurrentDateTime() => DateTime.Now;
}
