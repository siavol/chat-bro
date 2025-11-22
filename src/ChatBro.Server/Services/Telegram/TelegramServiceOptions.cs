using System.ComponentModel.DataAnnotations;

namespace ChatBro.Server.Services.Telegram;

public class TelegramServiceOptions
{
    [Required]
    [MinLength(1)]
    public required string Token { get; init; }
}

