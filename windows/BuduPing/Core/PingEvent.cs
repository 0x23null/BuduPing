namespace BuduPing.Core;

/// <summary>1 event cảm xúc nhận từ Firebase /pairs/{pairId}/inbox/{eventId}.</summary>
public sealed record PingEvent
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    /// <summary>Loại cảm xúc: love, miss, hug, kiss, tease, angry, sad, cheer, goodnight, sorry, funny, hungry, jealous...</summary>
    public string Type { get; init; } = "love";
    /// <summary>Text tuỳ chọn ≤ 80 ký tự, emoji giữ nguyên.</summary>
    public string Text { get; init; } = "";
    /// <summary>Tên người gửi (do app điện thoại cho đặt).</summary>
    public string From { get; init; } = "";
    public long Ts { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
