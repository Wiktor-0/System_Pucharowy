using HotChocolate.Authorization;
using Microsoft.EntityFrameworkCore;
using System_Pucharowy.Auth;
using System_Pucharowy.Data;
using System_Pucharowy.Models;

namespace System_Pucharowy.GraphQL;

public class Mutation
{
    // ========= AUTH =========

    public async Task<AuthPayload> Register(
        [Service] AppDbContext db,
        [Service] JwtTokenService jwt,
        string firstName,
        string lastName,
        string email,
        string password)
    {
        email = email.Trim().ToLower();

        if (await db.Users.AnyAsync(u => u.Email == email))
            throw new GraphQLException("Email already exists.");

        var user = new User
        {
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            PasswordHash = PasswordHasher.Hash(password)
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new AuthPayload
        {
            Token = jwt.CreateToken(user),
            User = user
        };
    }

    public async Task<AuthPayload> Login(
        [Service] AppDbContext db,
        [Service] JwtTokenService jwt,
        string email,
        string password)
    {
        email = email.Trim().ToLower();
        var hash = PasswordHasher.Hash(password);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.PasswordHash == hash);
        if (user is null)
            throw new GraphQLException("Invalid credentials.");

        return new AuthPayload
        {
            Token = jwt.CreateToken(user),
            User = user
        };
    }

    // ========= Tournament =========

    public async Task<Tournament> CreateTournament(
        [Service] AppDbContext db,
        string name,
        DateTime startDate)
    {
        var t = new Tournament
        {
            Name = name,
            StartDate = startDate,
            Status = "PLANNED"
        };

        db.Tournaments.Add(t);
        await db.SaveChangesAsync();
        return t;
    }

    public async Task<Tournament> addParticipant(
        [Service] AppDbContext db,
        int tournamentId,
        int userId)
    {
        var t = await db.Tournaments
            .Include(x => x.Participants)
            .Include(x => x.Bracket)!.ThenInclude(b => b.Matches)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t is null) throw new GraphQLException("Tournament not found.");
        if (t.Status != "PLANNED") throw new GraphQLException("Cannot add participants after start.");

        var u = await db.Users.FirstOrDefaultAsync(x => x.Id == userId);
        if (u is null) throw new GraphQLException("User not found.");

        if (t.Participants.Any(p => p.Id == u.Id))
            throw new GraphQLException("User already in tournament.");

        t.Participants.Add(u);
        await db.SaveChangesAsync();
        return t;
    }

    public async Task<Tournament> start(
        [Service] AppDbContext db,
        int tournamentId)
    {
        var t = await db.Tournaments
            .Include(x => x.Participants)
            .Include(x => x.Bracket)!.ThenInclude(b => b.Matches)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t is null) throw new GraphQLException("Tournament not found.");
        if (t.Bracket is null) throw new GraphQLException("Generate bracket first.");
        if (t.Participants.Count < 2) throw new GraphQLException("Need at least 2 participants.");

        t.Status = "STARTED";
        await db.SaveChangesAsync();
        return t;
    }

    public async Task<Tournament> finish(
        [Service] AppDbContext db,
        int tournamentId)
    {
        var t = await db.Tournaments
            .Include(x => x.Bracket)!.ThenInclude(b => b.Matches)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t is null) throw new GraphQLException("Tournament not found.");
        if (t.Bracket is null) throw new GraphQLException("No bracket.");

        var allPlayed = t.Bracket.Matches.All(m => m.Winner != null);
        if (!allPlayed) throw new GraphQLException("Not all matches are played.");

        t.Status = "FINISHED";
        await db.SaveChangesAsync();
        return t;
    }

    // ========= Bracket =========

    public async Task<Bracket> generateBracket(
        [Service] AppDbContext db,
        int tournamentId)
    {
        var t = await db.Tournaments
            .Include(x => x.Participants)
            .Include(x => x.Bracket)!.ThenInclude(b => b.Matches)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t is null) throw new GraphQLException("Tournament not found.");
        if (t.Status != "PLANNED") throw new GraphQLException("Bracket only before start.");
        if (t.Participants.Count < 2) throw new GraphQLException("Need at least 2 participants.");

        if (t.Bracket != null)
        {
            db.Matches.RemoveRange(t.Bracket.Matches);
            db.Brackets.Remove(t.Bracket);
            t.Bracket = null;
            await db.SaveChangesAsync();
        }

        var participants = t.Participants.ToList();

        if (participants.Count % 2 == 1)
            participants.RemoveAt(participants.Count - 1);

        var bracket = new Bracket();

        for (int i = 0; i < participants.Count; i += 2)
        {
            bracket.Matches.Add(new Match
            {
                Round = 1,
                Player1 = participants[i],
                Player2 = participants[i + 1],
                Winner = null
            });
        }

        t.Bracket = bracket;
        db.Brackets.Add(bracket);

        await db.SaveChangesAsync();
        return bracket;
    }

    public async Task<List<Match>> getMatchesForRound(
        [Service] AppDbContext db,
        int tournamentId,
        int round)
    {
        var t = await db.Tournaments
            .Include(x => x.Bracket)!.ThenInclude(b => b.Matches).ThenInclude(m => m.Player1)
            .Include(x => x.Bracket)!.ThenInclude(b => b.Matches).ThenInclude(m => m.Player2)
            .Include(x => x.Bracket)!.ThenInclude(b => b.Matches).ThenInclude(m => m.Winner)
            .FirstOrDefaultAsync(x => x.Id == tournamentId);

        if (t is null) throw new GraphQLException("Tournament not found.");
        if (t.Bracket is null) throw new GraphQLException("No bracket.");

        return t.Bracket.Matches.Where(m => m.Round == round).ToList();
    }

    // ========= Match =========

    public async Task<Match> play(
        [Service] AppDbContext db,
        int matchId,
        int winnerId)
    {
        var match = await db.Matches
            .Include(m => m.Player1)
            .Include(m => m.Player2)
            .Include(m => m.Winner)
            .FirstOrDefaultAsync(m => m.Id == matchId);

        if (match is null) throw new GraphQLException("Match not found.");
        if (match.Winner != null) throw new GraphQLException("Match already played.");

        if (winnerId != match.Player1.Id && winnerId != match.Player2.Id)
            throw new GraphQLException("Winner must be player1 or player2.");

        var winner = await db.Users.FirstAsync(u => u.Id == winnerId);
        match.Winner = winner;

        await db.SaveChangesAsync();
        return match;
    }
}

