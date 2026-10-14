using SupportDesk.Application.Auth;
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Application.Abstractions;

public interface ITokenIssuer
{
    IssuedToken Issue(Agent agent, DateTime nowUtc);
}
