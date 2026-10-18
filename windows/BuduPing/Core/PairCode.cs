using System.Security.Cryptography;
using System.Text;

namespace BuduPing.Core;

/// <summary>
/// Mã cặp BuduPing — implement Y HỆT docs/pair-code.md (dùng chung Windows + Android).
/// 7 ký tự payload Crockford Base32 + 1 ký tự checksum = tổng 8 ký tự, hiển thị XXXX-XXXX.
/// </summary>
public static class PairCode
{
    /// <summary>Bảng chữ Crockford Base32 rút gọn: đã loại I, L, O, U.</summary>
    public const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>Gen mã mới: 7 payload random đều (crypto) + checksum = Alphabet[sum(payload) mod 32]. Trả về raw 8 ký tự.</summary>
    public static string Generate()
    {
        Span<int> payload = stackalloc int[7];
        int sum = 0;
        for (int i = 0; i < 7; i++)
        {
            payload[i] = RandomNumberGenerator.GetInt32(32);
            sum += payload[i];
        }

        var chars = new char[8];
        for (int i = 0; i < 7; i++) chars[i] = Alphabet[payload[i]];
        chars[7] = Alphabet[sum % 32];
        return new string(chars);
    }

    /// <summary>Hiển thị cho người dùng: XXXX-XXXX (vd K7F3-9QX2).</summary>
    public static string ToDisplay(string raw)
        => raw.Length == 8 ? raw[..4] + "-" + raw[4..] : raw;

    /// <summary>pairId dùng trong Firebase = 8 ký tự đã chuẩn hoá, không dấu gạch.</summary>
    public static string ToPairId(string raw) => Normalize(raw);

    /// <summary>
    /// Chuẩn hoá input theo đúng thứ tự spec: viết hoa → O→0, I→1, L→1 → giữ lại ký tự thuộc bảng chữ.
    /// </summary>
    public static string Normalize(string? input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        var sb = new StringBuilder(input.Length);
        foreach (char c in input.ToUpperInvariant())
        {
            char m = c switch { 'O' => '0', 'I' => '1', 'L' => '1', _ => c };
            if (Alphabet.IndexOf(m) >= 0) sb.Append(m);
        }
        return sb.ToString();
    }

    /// <summary>Validate: sau chuẩn hoá phải đúng 8 ký tự và checksum ký tự cuối phải khớp.</summary>
    public static bool IsValid(string? input)
    {
        string raw = Normalize(input);
        if (raw.Length != 8) return false;
        int sum = 0;
        for (int i = 0; i < 7; i++) sum += Alphabet.IndexOf(raw[i]);
        return raw[7] == Alphabet[sum % 32];
    }
}
