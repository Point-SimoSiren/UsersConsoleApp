using Microsoft.Data.SqlClient;

// Muodostetaan yhteysosoite SQL Serveriin SqlConnectionStringBuilder-luokan avulla.
var connectionString = new SqlConnectionStringBuilder
    {
        DataSource = @"DUUNIKONE\SQLEXPRESS",
        InitialCatalog = "TaskDB",
        IntegratedSecurity = true,
        TrustServerCertificate = true
    }.ConnectionString;

try
{
    // Yritetään muodostaa yhteys SQL Serveriin käyttäen SqlConnection-luokkaa.
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("Yhteys SQL Serveriin muodostettu onnistuneesti.");

    // Käyttämällä SQL-kyselyä haetaan kaikki käyttäjät taulusta dbo.Users.
    const string sql = """
        SELECT UserId, FirstName, LastName, Email, CreatedAt
        FROM dbo.Users
        ORDER BY UserId;
        """;

    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    // Tulostetaan otsikot ja sarakkeiden nimet konsoliin.
    Console.WriteLine("Käyttäjät");
    Console.WriteLine(new string('-', 100));
    Console.WriteLine($"{"Id",-6} {"Etunimi",-18} {"Sukunimi",-18} {"Sähköposti",-32} {"Luotu"}");
    Console.WriteLine(new string('-', 100));

    var usersFound = false;

    // Käydään while loopissa läpi kaikki rivit, jotka reader palauttaa.
    // Jokaisella kierroksella luetaan rivin sarakkeet ja tulostetaan ne konsoliin.
    while (await reader.ReadAsync())
    {
        usersFound = true;

        var userId = reader.GetInt32(0);
        var firstName = reader.GetString(1);
        var lastName = reader.GetString(2);
        var email = reader.GetString(3);
        var createdAt = reader.GetDateTime(4);

        Console.WriteLine($"{userId,-6} {firstName,-18} {lastName,-18} {email,-32} {createdAt:dd.MM.yyyy HH:mm}");
    }

    if (!usersFound)
    {
        Console.WriteLine("Käyttäjiä ei löytynyt.");
    }
}
// Jos SQL Serveriin yhdistäminen epäonnistuu, käsitellään poikkeus SqlException-luokan avulla.
catch (SqlException exception)
{
    Console.Error.WriteLine("Yhteyden muodostaminen SQL Serveriin epäonnistui.");
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 1;
}
