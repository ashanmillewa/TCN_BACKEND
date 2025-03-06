namespace TCN_WebAPI.DbOperations
{
    public interface IDbOps
    {
        List<(string?, string?, string?, string?, string?, string?)> GetByPhoneNumber(string clientID, string? search_IdPHCAL = null);

        List<(string?, string?, string?, string?, string?, string?)> GetClientCallRecordsByAgent(string clientID, string Search_IDORREC = null);

        List<(string?, string?, string?, string?, string?, string?)> GetByDate(string clientID, string? startDate = null, string? endDate = null);

    }
}
