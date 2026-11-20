namespace Reboot.Data;

public static class ConnectionHelper
{
    public static string FormatPostgreSqlConnectionString(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        raw = raw.Trim();

        // Check if already in key=value format
        if (raw.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
            raw.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            if (!raw.Contains("SSL Mode=", StringComparison.OrdinalIgnoreCase))
            {
                raw += ";SSL Mode=Require;Trust Server Certificate=true;";
            }
            return raw;
        }

        // Handle postgresql:// or postgres:// URI
        if (raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Remove protocol
                var schemeEnd = raw.IndexOf("://", StringComparison.Ordinal);
                var withoutScheme = raw[(schemeEnd + 3)..];

                // Split by last '@' to separate user:pass from host:port/db
                var lastAt = withoutScheme.LastIndexOf('@');
                if (lastAt == -1) return raw;

                var userPass = withoutScheme[..lastAt];
                var hostPortDb = withoutScheme[(lastAt + 1)..];

                // user:pass
                var colonIndex = userPass.IndexOf(':');
                var username = colonIndex != -1 ? Uri.UnescapeDataString(userPass[..colonIndex]) : userPass;
                var password = colonIndex != -1 ? Uri.UnescapeDataString(userPass[(colonIndex + 1)..]) : "";

                // host:port/db
                var slashIndex = hostPortDb.IndexOf('/');
                var hostPort = slashIndex != -1 ? hostPortDb[..slashIndex] : hostPortDb;
                var database = slashIndex != -1 ? hostPortDb[(slashIndex + 1)..] : "postgres";

                // Strip any query parameters (?sslmode=...)
                var queryIndex = database.IndexOf('?');
                if (queryIndex != -1) database = database[..queryIndex];
                if (string.IsNullOrEmpty(database)) database = "postgres";

                var portColon = hostPort.IndexOf(':');
                var host = portColon != -1 ? hostPort[..portColon] : hostPort;
                var port = portColon != -1 && int.TryParse(hostPort[(portColon + 1)..], out var parsedPort) ? parsedPort : 5432;

                return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
            }
            catch
            {
                return raw;
            }
        }

        return raw;
    }
}
