using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5050");

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

const int MaxPlayers = 40;
const int QuestionSeconds = 60;

var users = new ConcurrentDictionary<string, UserAccount>(StringComparer.OrdinalIgnoreCase);
var rooms = new ConcurrentDictionary<string, RaceRoom>(StringComparer.OrdinalIgnoreCase);

app.MapGet("/api/server-info", (HttpContext ctx) =>
{
    var host = ctx.Request.Host.Host;
    if (host is "localhost" or "127.0.0.1")
    {
        var lanIp = GetLocalIPv4();
        if (!string.IsNullOrEmpty(lanIp))
            host = lanIp;
    }
    var port = ctx.Request.Host.Port ?? 5050;
    return Results.Ok(new { baseUrl = $"{ctx.Request.Scheme}://{host}:{port}" });
});

app.MapPost("/api/auth/register", (RegisterReq req) =>
{
    if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Pass))
        return Results.BadRequest(new { error = "Бүх талбарыг бөглөнө үү." });

    var key = req.Username.Trim().ToLowerInvariant();
    var account = new UserAccount(Guid.NewGuid().ToString("N")[..10], req.Name.Trim(), key, req.Pass, req.Role ?? "student");
    if (!users.TryAdd(key, account))
        return Results.Conflict(new { error = "Энэ нэвтрэх нэр бүртгэлтэй байна." });

    return Results.Ok(new { account.Id, account.Name, account.Username, account.Role });
});

app.MapPost("/api/auth/login", (LoginReq req) =>
{
    var key = (req.Username ?? "").Trim().ToLowerInvariant();
    if (!users.TryGetValue(key, out var account) || account.Pass != req.Pass)
        return Results.Unauthorized();

    return Results.Ok(new { account.Id, account.Name, account.Username, account.Role });
});

app.MapPost("/api/rooms", (CreateRoomReq req) =>
{
    var code = GenerateRoomCode();
    var room = new RaceRoom
    {
        Code = code,
        Title = string.IsNullOrWhiteSpace(req.Title) ? "Ангийн уралдаан" : req.Title.Trim(),
        HostId = req.HostId ?? "host",
        Questions = req.Questions ?? [],
        Status = "lobby",
        CurrentQ = 0,
        QStartTimeMs = 0
    };
    rooms[code] = room;
    return Results.Ok(room.ToDto());
});

app.MapGet("/api/rooms/{code}", (string code) =>
{
    if (!rooms.TryGetValue(code.ToUpperInvariant(), out var room))
        return Results.NotFound(new { error = "Өрөө олдсонгүй." });
    lock (room.Sync)
    {
        return Results.Ok(room.ToDto());
    }
});

app.MapPost("/api/rooms/{code}/join", (string code, JoinRoomReq req) =>
{
    if (!rooms.TryGetValue(code.ToUpperInvariant(), out var room))
        return Results.NotFound(new { error = "Ийм кодтой өрөө олдсонгүй." });

    lock (room.Sync)
    {
        if (room.Players.Count >= MaxPlayers)
            return Results.BadRequest(new { error = $"Өрөө дүүрсэн байна (Дээд тал нь {MaxPlayers} хүн)." });
        if (room.Status == "finished")
            return Results.BadRequest(new { error = "Энэ уралдаан аль хэдийн дууссан байна." });

        var player = new RacePlayer
        {
            Id = Guid.NewGuid().ToString("N")[..10],
            Name = string.IsNullOrWhiteSpace(req.Name) ? "Сурагч" : req.Name.Trim(),
            Avatar = req.Avatar ?? new AvatarConfig(),
            IsBot = req.IsBot
        };
        room.Players.Add(player);
        return Results.Ok(new { player, room = room.ToDto() });
    }
});

app.MapPost("/api/rooms/{code}/start", (string code) =>
{
    if (!rooms.TryGetValue(code.ToUpperInvariant(), out var room))
        return Results.NotFound();
    lock (room.Sync)
    {
        room.Status = "racing";
        room.CurrentQ = 0;
        room.QStartTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return Results.Ok(room.ToDto());
    }
});

app.MapPost("/api/rooms/{code}/answer", (string code, AnswerReq req) =>
{
    if (!rooms.TryGetValue(code.ToUpperInvariant(), out var room))
        return Results.NotFound();

    lock (room.Sync)
    {
        if (room.Status != "racing") return Results.BadRequest();
        var player = room.Players.FirstOrDefault(p => p.Id == req.PlayerId);
        if (player is null || player.Answers.ContainsKey(room.CurrentQ))
            return Results.Ok(room.ToDto());

        var elapsedSec = (int)Math.Floor((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - room.QStartTimeMs) / 1000.0);
        var remSec = Math.Max(0, QuestionSeconds - elapsedSec);
        var q = room.Questions[room.CurrentQ];
        var ok = req.ChoiceIdx == q.Correct;
        var pts = ok ? remSec : 0;

        player.Answers[room.CurrentQ] = new PlayerAnswer(pts, ok);
        player.Score += pts;
        if (ok) player.CorrectCount += 1;

        return Results.Ok(new { pts, ok, remSec, room = room.ToDto() });
    }
});

app.MapPost("/api/rooms/{code}/next", (string code) =>
{
    if (!rooms.TryGetValue(code.ToUpperInvariant(), out var room))
        return Results.NotFound();

    lock (room.Sync)
    {
        if (room.CurrentQ + 1 >= room.Questions.Count)
        {
            room.Status = "finished";
        }
        else
        {
            room.CurrentQ += 1;
            room.QStartTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
        return Results.Ok(room.ToDto());
    }
});

app.Run();

static string GenerateRoomCode()
{
    const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
}

static string? GetLocalIPv4()
{
    try
    {
        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                return ip.ToString();
        }
    }
    catch { }
    return null;
}

record UserAccount(string Id, string Name, string Username, string Pass, string Role);
record RegisterReq(string Name, string Username, string Pass, string? Role);
record LoginReq(string Username, string Pass);
record QuestionItem(string Text, List<string> Opts, int Correct);
record CreateRoomReq(string Title, string? HostId, List<QuestionItem> Questions);
record AvatarConfig(string Outfit = "hoodie", string Color = "#3b5bfd", string Hat = "cap", string Face = "smile", string Skin = "#fbdcc4");
record JoinRoomReq(string Name, AvatarConfig? Avatar, bool IsBot = false);
record AnswerReq(string PlayerId, int ChoiceIdx);
record PlayerAnswer(int Pts, bool Ok);

class RacePlayer
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public AvatarConfig Avatar { get; set; } = new();
    public int Score { get; set; }
    public int CorrectCount { get; set; }
    public Dictionary<int, PlayerAnswer> Answers { get; set; } = new();
    public bool IsBot { get; set; }
}

class RaceRoom
{
    public object Sync { get; } = new();
    public string Code { get; set; } = "";
    public string Title { get; set; } = "";
    public string HostId { get; set; } = "";
    public List<QuestionItem> Questions { get; set; } = [];
    public string Status { get; set; } = "lobby";
    public int CurrentQ { get; set; }
    public long QStartTimeMs { get; set; }
    public List<RacePlayer> Players { get; set; } = [];

    public object ToDto() => new
    {
        code = Code,
        title = Title,
        hostId = HostId,
        questions = Questions,
        status = Status,
        currentQ = CurrentQ,
        qStartTime = QStartTimeMs,
        players = Players.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            avatar = p.Avatar,
            score = p.Score,
            correctCount = p.CorrectCount,
            answers = p.Answers,
            isBot = p.IsBot
        })
    };
}
