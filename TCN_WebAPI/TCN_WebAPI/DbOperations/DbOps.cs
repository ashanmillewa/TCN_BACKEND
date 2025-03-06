using Serilog;
using System.Data.Common;
using System.Data;
using System.Data.Odbc;
using System.Text;
using System.Globalization;

namespace TCN_WebAPI.DbOperations
{
    public class DbOps : IDbOps
    {
        private readonly IConfiguration _config;

        public DbOps(IConfiguration config)
        {
            _config = config;
        }

        public List<(string?, string?, string?, string?, string?, string?)> GetByPhoneNumber(string clientID, string? search_IdPHCAL)
        {
            List<(string?, string?, string?, string?, string?, string?)> audioFiles = new List<(string?, string?, string?, string?, string?, string?)>();

            OdbcConnection? connection = null;
            try
            {
                var odbcConnections = _config.GetSection("AppData:GUIODBCConnections").Get<List<Dictionary<string, string>>>() ?? new List<Dictionary<string, string>>();
                var connectionInfo = odbcConnections.FirstOrDefault(connection => connection.TryGetValue("ClientID", out string? ClientID) && ClientID.ToUpper() == clientID.ToUpper());

                if (connectionInfo != null && connectionInfo.TryGetValue("ConnectionString", out string? connString))
                {
                    string? decryptedConString = DecodeBase64String(connString);

                    using (connection = new OdbcConnection(decryptedConString))
                    {
                        connection.Open();

                        // Add filtering condition
                        string selectQuery = "SELECT IDPHCAL, IDINOUT, IDDATE, IDTIME, IDORREC, IDRECPT FROM SCITCREC WHERE IDPHCAL LIKE ?";

                        using (OdbcCommand cmd = new OdbcCommand(selectQuery, connection))
                        {
                            if (!string.IsNullOrEmpty(search_IdPHCAL))
                            {
                                cmd.Parameters.AddWithValue("IDPHCAL", "%" + search_IdPHCAL + "%");
                            }

                            using (OdbcDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string? IDPHCAL = reader.IsDBNull(0) ? null : reader[0].ToString();
                                    string? IDINOUT = reader.IsDBNull(1) ? null : reader[1].ToString();
                                    string? IDDATE = reader.IsDBNull(2) ? null : reader[2].ToString();
                                    string? IDTIME = reader.IsDBNull(3) ? null : reader[3].ToString();
                                    string? IDORREC = reader.IsDBNull(4) ? null : reader[4].ToString();
                                    string? IDRECPT = reader.IsDBNull(5) ? null : reader[5].ToString();

                                    audioFiles.Add((IDPHCAL, IDINOUT, IDDATE, IDTIME, IDORREC, IDRECPT));
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("ConnectionString Invalid");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error occurred while fetching records.");
            }
            finally
            {
                if (connection != null && connection.State == ConnectionState.Open)
                {
                    connection.Close();
                }
            }

            return audioFiles;
        }

        public List<(string?, string?, string?, string?, string?, string?)> GetClientCallRecordsByAgent(string clientID, string Search_IDORREC = null)
        {
            List<(string?, string?, string?, string?, string?, string?)> AudioFile = new List<(string?, string?, string?, string?, string?, string?)>();

            OdbcConnection? connection = null;
            try
            {
                var odbcConnections = _config.GetSection("AppData:GUIODBCConnections").Get<List<Dictionary<string, string>>>() ?? new List<Dictionary<string, string>>();
                var connectionInfo = odbcConnections.FirstOrDefault(connection => connection.TryGetValue("ClientID", out string? ClientID) && ClientID.ToUpper() == clientID.ToUpper());

                if (connectionInfo != null && connectionInfo.TryGetValue("ConnectionString", out string? connString))
                {
                    string? decryptedConString = DecodeBase64String(connString);

                    using (connection = new OdbcConnection(decryptedConString))
                    {
                        connection.Open();

                        // Modify the query to include filtering on IDORREC
                        string selectQuery = "SELECT IDPHCAL, IDINOUT, IDDATE, IDTIME, IDORREC, IDRECPT FROM SCITCREC WHERE IDORREC LIKE ?";

                        using (OdbcCommand cmd = new OdbcCommand(selectQuery, connection))
                        {
                            // Add parameter for the Search_IDORREC if provided
                            if (!string.IsNullOrEmpty(Search_IDORREC))
                            {
                                cmd.Parameters.Add(new OdbcParameter("IDORREC", "%" + Search_IDORREC + "%"));
                            }

                            using (OdbcDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string? IDPHCAL = reader.IsDBNull(0) ? null : reader[0].ToString();
                                    string? IDINOUT = reader.IsDBNull(1) ? null : reader[1].ToString();
                                    string? IDDATE = reader.IsDBNull(2) ? null : reader[2].ToString();
                                    string? IDTIME = reader.IsDBNull(3) ? null : reader[3].ToString();
                                    string? IDORREC = reader.IsDBNull(4) ? null : reader[4].ToString();
                                    string? IDRECPT = reader.IsDBNull(5) ? null : reader[5].ToString();

                                    AudioFile.Add((IDPHCAL, IDINOUT, IDDATE, IDTIME, IDORREC, IDRECPT));
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("ConnectionString Invalid");
                }
            }
            catch (Exception ex)
            {
                string emsg = ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source;
                Log.Error(emsg);
            }
            finally
            {
                if (connection != null && connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return AudioFile;
        }

        public List<(string?, string?, string?, string?, string?, string?)> GetByDate(string clientID, string? startDate = null, string? endDate = null)
        {
            List<(string?, string?, string?, string?, string?, string?)> AudioFile = new List<(string?,string?, string?, string?, string?, string?)>();

            OdbcConnection? connection = null;
            try
            {
                var odbcConnections = _config.GetSection("AppData:GUIODBCConnections").Get<List<Dictionary<string, string>>>() ?? new List<Dictionary<string, string>>();
                var connectionInfo = odbcConnections.FirstOrDefault(connection => connection.TryGetValue("ClientID", out string? ClientID) && ClientID.ToUpper() == clientID.ToUpper());

                if (connectionInfo != null && connectionInfo.TryGetValue("ConnectionString", out string? connString))
                {
                    string? decryptedConString = DecodeBase64String(connString);

                    using (connection = new OdbcConnection(decryptedConString))
                    {
                        connection.Open();

                        string selectQuery = "SELECT IDPHCAL, IDINOUT, IDDATE, IDTIME, IDORREC, IDRECPT FROM SCITCREC";

                        // Add date range filter if startDate and endDate are provided
                        if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                        {
                            selectQuery += " WHERE IDDATE BETWEEN ? AND ?";
                        }
                        else if (!string.IsNullOrEmpty(startDate))
                        {
                            selectQuery += " WHERE IDDATE >= ?";
                        }
                        else if (!string.IsNullOrEmpty(endDate))
                        {
                            selectQuery += " WHERE IDDATE <= ?";
                        }

                        using (OdbcCommand cmd = new OdbcCommand(selectQuery, connection))
                        {
                            // Add parameters for filtering by date range
                            if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                            {
                                cmd.Parameters.Add(new OdbcParameter("StartDate", startDate));
                                cmd.Parameters.Add(new OdbcParameter("EndDate", endDate));
                            }
                            else if (!string.IsNullOrEmpty(startDate))
                            {
                                cmd.Parameters.Add(new OdbcParameter("StartDate", startDate));
                            }
                            else if (!string.IsNullOrEmpty(endDate))
                            {
                                cmd.Parameters.Add(new OdbcParameter("EndDate", endDate));
                            }

                            using (OdbcDataReader reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    string? IDPHCAL = reader.IsDBNull(0) ? null : reader[0].ToString();
                                    string? IDINOUT = reader.IsDBNull(1) ? null : reader[1].ToString();
                                    string? IDDATE = reader.IsDBNull(2) ? null : reader[2].ToString();
                                    string? IDTIME = reader.IsDBNull(3) ? null : reader[3].ToString();
                                    string? IDORREC = reader.IsDBNull(4) ? null : reader[4].ToString();
                                    string? IDRECPT = reader.IsDBNull(5) ? null : reader[5].ToString();

                                    AudioFile.Add((IDPHCAL, IDINOUT, IDDATE, IDTIME, IDORREC, IDRECPT));
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log.Error("ConnectionString Invalid");
                }
            }
            catch (Exception ex)
            {
                string emsg = ex.Message + "\n" + ex.StackTrace + "\n" + ex.Source;
                Log.Error(emsg);
            }
            finally
            {
                if (connection != null && connection.State == ConnectionState.Open)
                    connection.Close();
            }

            return AudioFile;
        }

        public static string? DecodeBase64String(string base64EncodedText)
        {
            if (string.IsNullOrEmpty(base64EncodedText))
            {
                return null;
            }

            try
            {
                byte[] decodedBytes = Convert.FromBase64String(base64EncodedText);
                return Encoding.UTF8.GetString(decodedBytes);
            }
            catch (FormatException ex)
            {
                Log.Error("Error:" + ex);
                return null;
            }
        }

    }
}
