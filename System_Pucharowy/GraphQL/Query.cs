using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System_Pucharowy.Data;
using System_Pucharowy.Models;

namespace System_Pucharowy.GraphQL;

public class Query
{

    public async Task<User> Me([Service] AppDbContext db, ClaimsPrincipal claims)
    {
        var id = int.Parse(claims.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await db.Users.FirstAsync(u => u.Id == id);
    }

    public async Task<List<Tournament>> Tournaments([Service] AppDbContext db)
        => await db.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Bracket)!.ThenInclude(b => b.Matches)
            .ToListAsync();

    public async Task<Tournament?> Tournament([Service] AppDbContext db, int id)
        => await db.Tournaments
            .Include(t => t.Participants)
            .Include(t => t.Bracket)!.ThenInclude(b => b.Matches)
            .FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<Match>> MyMatches([Service] AppDbContext db, ClaimsPrincipal claims, bool? played)
    {
        var myId = int.Parse(claims.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var q = db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .Where(m => m.Player1.Id == myId || m.Player2.Id == myId);

        if (played == true) q = q.Where(m => m.Winner != null);
        if (played == false) q = q.Where(m => m.Winner == null);

        return await q.ToListAsync();
    }
}

