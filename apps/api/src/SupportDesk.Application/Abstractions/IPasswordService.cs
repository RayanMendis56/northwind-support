using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Application.Abstractions;

public interface IPasswordService
{
    string Hash(Agent agent, string password);

    /// <summary>
    /// False for a wrong password, an agent with no password, or no agent at all. In every case
    /// it does the same amount of hashing work, so timing does not reveal which.
    /// </summary>
    bool Verify(Agent? agent, string password);
}
