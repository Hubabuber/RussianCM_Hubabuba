using Discord;

namespace Content.DiscordBot;

public static class Logger
{
    public static Task Log(LogMessage msg)
    {
        var message = $"[{msg.Severity.ToString().ToUpper()}] {msg.Source}: {msg.Message}";
        if (msg.Exception != null)
            message += $"\n{msg.Exception}";

        return Console.Out.WriteLineAsync(message);
    }

    public static Task Info(string msg)
    {
        return Console.Out.WriteLineAsync($"[INFO] {msg}");
    }

    public static Task Error(string msg, Exception e)
    {
        return Console.Out.WriteLineAsync($"[ERROR] {msg}\n{e}");
    }
}
