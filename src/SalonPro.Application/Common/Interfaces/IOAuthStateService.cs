namespace SalonPro.Application.Common.Interfaces;

/// <summary>
/// Signed OAuth state for Meta redirect (tenant id survives app pool restarts).
/// </summary>
public interface IOAuthStateService
{
    string CreateState(Guid tenantId);
    Guid ParseState(string state);
}
