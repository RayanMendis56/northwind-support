using Microsoft.AspNetCore.Identity;
using SupportDesk.Application.Abstractions;
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Infrastructure.Services;

public sealed class PasswordService : IPasswordService
{
    private static readonly Agent Placeholder = new("Placeholder", "placeholder@invalid", 1, DateTime.UnixEpoch);

    private readonly PasswordHasher<Agent> _hasher = new();
    private readonly string _dummyHash;

    public PasswordService() => _dummyHash = _hasher.HashPassword(Placeholder, Guid.NewGuid().ToString("N"));

    public string Hash(Agent agent, string password) => _hasher.HashPassword(agent, password);

    public bool Verify(Agent? agent, string password)
    {
        if (agent?.PasswordHash is null)
        {
            _hasher.VerifyHashedPassword(Placeholder, _dummyHash, password);
            return false;
        }

        return _hasher.VerifyHashedPassword(agent, agent.PasswordHash, password)
            is not PasswordVerificationResult.Failed;
    }
}
