namespace Content.DiscordBot;

public sealed class Config
{
    public string Token { get; set; } = string.Empty;

    public string DatabaseString { get; set; } = string.Empty;

    public string DatabaseEngine { get; set; } = "postgres";

    public ulong Guild { get; set; } = 1168210010233376858UL;
}
