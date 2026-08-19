using System.Security.Cryptography;
using KIPL.AssetManagement.Application.Common.Interfaces;
using KIPL.AssetManagement.Domain.Enums;
using KIPL.AssetManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace KIPL.AssetManagement.Infrastructure.Services;

/// <summary>
/// Produces tags as AST- followed by random digits, e.g. AST-284917 — the same
/// prefix used everywhere else an asset's system ID is shown, so a printed tag
/// and the on-screen Asset ID read as one consistent scheme.
/// </summary>
public class TagGenerator : ITagGenerator
{
    private const string Prefix = "AST";
    private const string Digits = "0123456789";
    private readonly ApplicationDbContext _db;

    public TagGenerator(ApplicationDbContext db) => _db = db;

    public string NewAssetTag(AssetCategory category)
    {
        for (var attempt = 0; attempt < 12; attempt++)
        {
            var tag = $"{Prefix}-{RandomDigits(6)}";
            if (!_db.Assets.Any(a => a.Tag == tag)) return tag;
        }

        // Extremely unlikely; fall back to a longer suffix rather than loop forever.
        return $"{Prefix}-{RandomDigits(10)}";
    }

    public string NewReturnReference() => NextSequential("RET", _db.AssetReturns.Select(r => r.Reference), 2045);
    public string NewServiceReference() => NextSequential("SR", _db.ServiceRequests.Select(s => s.Reference), 1041);

    private static string NextSequential(string prefix, IQueryable<string> existing, int seed)
    {
        var numbers = existing
            .Where(r => r.StartsWith(prefix + "-"))
            .ToList()
            .Select(r => int.TryParse(r[(prefix.Length + 1)..], out var n) ? n : 0);

        var max = numbers.DefaultIfEmpty(seed - 1).Max();
        return $"{prefix}-{Math.Max(max + 1, seed)}";
    }

    private static string RandomDigits(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Digits[RandomNumberGenerator.GetInt32(Digits.Length)];
        return new string(chars);
    }
}
