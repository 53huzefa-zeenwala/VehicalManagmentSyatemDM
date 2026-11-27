using System.Security.Cryptography;
using System.Text;

namespace VehicleManagementDM.Api.Common.Auth;

/// <summary>
/// Passwords are stored as plain text, by owner decision (D-051). They are compared here, in code, and
/// never in SQL: the database collation is case-insensitive, so a SQL comparison would accept "secret" for "SECRET".
/// The comparison takes the same time wherever the first difference is.
/// </summary>
public static class Passwords
{
    public static bool Matches(string stored, string given) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(stored), Encoding.UTF8.GetBytes(given));
}
