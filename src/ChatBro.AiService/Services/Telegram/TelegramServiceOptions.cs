using System.ComponentModel.DataAnnotations;

namespace ChatBro.AiService.Services.Telegram;

public class TelegramServiceOptions
{
    [Required]
    [MinLength(1)]
    public required string Token { get; init; }
}
