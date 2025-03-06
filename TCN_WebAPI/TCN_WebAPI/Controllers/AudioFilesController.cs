using Microsoft.AspNetCore.Mvc;
using TCN_WebAPI.DbOperations;
using Serilog;
using TCN_WebAPI.Models;
using System.Globalization;


namespace TCN_WebAPI.Controllers
{
    [ApiController]
    [Route("/api/v1/[controller]")]
    public class AudioFilesController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        readonly IDbOps _iops;

        public AudioFilesController(IDbOps iops,  IConfiguration configuration)
        {
            _configuration = configuration;
            _iops = iops ?? throw new ArgumentNullException(nameof(iops));
        }

        List<(string IDPHCAL, string IDINOUT, string IDDATE, string IDORREC, string IDRECPT)> codeDescriptions = new List<(string IDPHCAL, string IDINOUT, string IDDATE, string IDORREC, string IDRECPT)>();

        [HttpGet]
        [Route("/api/v1/[controller]/GetByPhoneNumber")]
        public async Task<IActionResult> GetByPhoneNumber(string search_IdPHCAL = null)
        {
            Log.Information("------------------------------------------");
            Log.Information("Get Call Record Details using PhoneNumber.");
            Log.Information("------------------------------------------");

            try
            {
                // Fetch all ClientIDs and their connection strings from appsettings
                var clientConnections = _configuration
                    .GetSection("AppData:GUIODBCConnections")
                    .GetChildren()
                    .Select(c => new
                    {
                        ClientID = c.GetValue<string>("ClientID"),
                        ConnectionString = c.GetValue<string>("ConnectionString")
                    }).ToList();

                if (!clientConnections.Any())
                {
                    Log.Warning("No client connections were found in the configuration.");
                    return NotFound(new { Message = "No client connections found" });
                }

                Log.Information($"Total client connections found: {clientConnections.Count}");

                // Retrieve records for all clients
                var allRecords = new List<AudioFile>();

                // Loop through each client connection and retrieve records
                foreach (var client in clientConnections)
                {
                    Log.Information($"Processing records for ClientID: {client.ClientID}");

                    try
                    {
                        // Fetch base URL from the appsettings
                        var baseUrl = _configuration.GetValue<string>("AppData:BaseUrl");

                        // Fetch records by Phone Number for the current client
                        var dbRecords = _iops.GetByPhoneNumber(client.ClientID, search_IdPHCAL);

                        if (dbRecords == null || !dbRecords.Any())
                        {
                            Log.Warning($"No records found for ClientID: {client.ClientID} with search_IdPHCAL: {search_IdPHCAL}");
                        }
                        else
                        {
                            Log.Information($"Fetched {dbRecords.Count} records for ClientID: {client.ClientID}.");
                        }

                        // Process the records and map to AudioFile
                        var records = dbRecords.Select(r => new AudioFile
                        {
                            IDPHCAL = r.Item1 ?? string.Empty,
                            IDINOUT = r.Item2 ?? string.Empty,
                            IDDATE = string.IsNullOrEmpty(r.Item3)
                                ? DateTime.MinValue
                                : DateTime.ParseExact(r.Item3, "yyyyMMdd", CultureInfo.InvariantCulture),
                            IDTIME = r.Item4 ?? string.Empty,
                            IDORREC = r.Item5 ?? string.Empty,
                            IDRECPT = string.IsNullOrEmpty(r.Item6) ? string.Empty : Path.Combine(baseUrl, r.Item6.Replace("C:\\", string.Empty)),
                        }).ToList();

                        // Add records to the final list
                        allRecords.AddRange(records);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"An error occurred while processing records for ClientID: {client.ClientID}");
                    }
                }

                if (allRecords.Any())
                {
                    Log.Information($"Total records obtained across all clients: {allRecords.Count}");
                }
                else
                {
                    Log.Warning("No records found across all clients.");
                }

                Log.Information("----------------------------------------");
                Log.Information("GetByPhoneNumber API Execution Completed");
                Log.Information("----------------------------------------");
                return Ok(allRecords);
            }
            catch (Exception ex)
            {
                Log.Error($"###   Error occurred while fetching call record details. Exception: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while processing the request." });
            }
        }

        [HttpGet]
        [Route("/api/v1/[controller]/GetByAgent")]
        public async Task<IActionResult> GetByAgent(string Search_IDORREC = null)
        {
            Log.Information("------------------------------------");
            Log.Information("Get Call Record Details using Agent.");
            Log.Information("------------------------------------");

            try
            {
                // Fetch base URL from the appsettings
                var baseUrl = _configuration.GetValue<string>("AppData:BaseUrl");

                // Fetch all ClientIDs and their connection strings from appsettings
                var clientConnections = _configuration
                    .GetSection("AppData:GUIODBCConnections")
                    .GetChildren()
                    .Select(c => new
                    {
                        ClientID = c.GetValue<string>("ClientID"),
                        ConnectionString = c.GetValue<string>("ConnectionString")
                    }).ToList();

                if (!clientConnections.Any())
                {
                    Log.Warning("No client connections found in the configuration.");
                    return NotFound(new { Message = "No client connections found" });
                }

                // Retrieve records for all clients
                var allRecords = new List<AudioFile>();

                // Use foreach for multiple clients
                foreach (var client in clientConnections)
                {
                    Log.Information($"Processing ClientID: {client.ClientID}");

                    try
                    {
                        // Get records for the current ClientID with optional filtering on IDORREC
                        var dbRecords = _iops.GetClientCallRecordsByAgent(client.ClientID, Search_IDORREC);

                        if (dbRecords == null || !dbRecords.Any())
                        {
                            Log.Warning($"No records found for ClientID: {client.ClientID}");
                        }

                        var records = dbRecords.Select(r => new AudioFile
                        {
                            IDPHCAL = r.Item1 ?? string.Empty,
                            IDINOUT = r.Item2 ?? string.Empty,
                            IDDATE = string.IsNullOrEmpty(r.Item3)
                                ? DateTime.MinValue
                                : DateTime.ParseExact(r.Item3, "yyyyMMdd", CultureInfo.InvariantCulture),
                            IDTIME = r.Item4 ?? string.Empty,
                            IDORREC = r.Item5 ?? string.Empty,
                            IDRECPT = string.IsNullOrEmpty(r.Item6) ? string.Empty : Path.Combine(baseUrl, r.Item6.Replace("C:\\", string.Empty)),
                        }).ToList();

                        allRecords.AddRange(records);
                        Log.Information($"Successfully processed records for ClientID: {client.ClientID}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"###   Error processing records for ClientID: {client.ClientID}. Exception: {ex.Message}");
                    }
                }

                if (allRecords.Any())
                {
                    Log.Information($"Successfully retrieved {allRecords.Count} total records.");
                }
                else
                {
                    Log.Warning("No records were retrieved.");
                }

                Log.Information("----------------------------------");
                Log.Information("GetByAgent API Execution Completed");
                Log.Information("----------------------------------");
                return Ok(allRecords);
            }
            catch (Exception ex)
            {
                Log.Error($"###   Error occurred while fetching call record details. Exception: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while processing the request." });
            }
        }

        [HttpGet]
        [Route("/api/v1/[controller]/GetByDate")]
        public async Task<IActionResult> GetByDate(string startDate = null, string endDate = null)
        {
            Log.Information("------------------------------------");
            Log.Information("Get Call Record Details using Date.");
            Log.Information("------------------------------------");

            try
            {
                // Fetch base URL from the appsettings
                var baseUrl = _configuration.GetValue<string>("AppData:BaseUrl");

                // Fetch all ClientIDs and their connection strings from appsettings
                var clientConnections = _configuration
                    .GetSection("AppData:GUIODBCConnections")
                    .GetChildren()
                    .Select(c => new
                    {
                        ClientID = c.GetValue<string>("ClientID"),
                        ConnectionString = c.GetValue<string>("ConnectionString")
                    }).ToList();

                if (!clientConnections.Any())
                {
                    Log.Warning("No client connections found in the configuration.");
                    return NotFound(new { Message = "No client connections found" });
                }

                // Retrieve records for all clients
                var allRecords = new List<AudioFile>();

                // Use foreach for multiple clients
                foreach (var client in clientConnections)
                {
                    Log.Information($"Processing ClientID: {client.ClientID}");

                    try
                    {
                        // Get records for the current ClientID, passing startDate and endDate
                        var dbRecords = _iops.GetByDate(client.ClientID, startDate, endDate);

                        if (dbRecords == null || !dbRecords.Any())
                        {
                            Log.Warning($"No records found for ClientID: {client.ClientID} within the date range {startDate} to {endDate}.");
                        }

                        var records = dbRecords.Select(r => new AudioFile
                        {
                            IDPHCAL = r.Item1 ?? string.Empty,
                            IDINOUT = r.Item2 ?? string.Empty,
                            IDDATE = string.IsNullOrEmpty(r.Item3)
                                ? DateTime.MinValue
                                : DateTime.ParseExact(r.Item3, "yyyyMMdd", CultureInfo.InvariantCulture),
                            IDTIME = r.Item4 ?? string.Empty,
                            IDORREC = r.Item5 ?? string.Empty,
                            IDRECPT = string.IsNullOrEmpty(r.Item6) ? string.Empty : Path.Combine(baseUrl, r.Item6.Replace("C:\\", string.Empty)),
                        }).ToList();

                        allRecords.AddRange(records);
                        Log.Information($"Successfully processed records for ClientID: {client.ClientID}.");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"###   Error processing records for ClientID: {client.ClientID}. Exception: {ex.Message}");
                    }
                }

                if (allRecords.Any())
                {
                    Log.Information($"Successfully retrieved {allRecords.Count} total records.");
                }
                else
                {
                    Log.Warning("No records were retrieved.");
                }

                Log.Information("---------------------------------");
                Log.Information("GetByDate API Execution Completed");
                Log.Information("---------------------------------");
                return Ok(allRecords);
            }
            catch (Exception ex)
            {
                Log.Error($"###   Error occurred while fetching call record details by date. Exception: {ex.Message}");
                return StatusCode(500, new { Message = "An error occurred while processing the request." });
            }
        }

    }
}













