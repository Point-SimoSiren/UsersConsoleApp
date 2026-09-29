using Microsoft.Data.SqlClient;
using System.Net.Mail;

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
    // await using huolehtii siitä, että yhteys suljetaan ohjelman päättyessä.
    await using var connection = new SqlConnection(connectionString);
    await connection.OpenAsync();
    Console.WriteLine("Yhteys SQL Serveriin muodostettu onnistuneesti.");

    // Näytetään valikko niin kauan, kunnes käyttäjä valitsee ohjelman lopettamisen.
    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("1. Näytä käyttäjät");
        Console.WriteLine("2. Lisää käyttäjä");
        Console.WriteLine("0. Lopeta");
        Console.Write("Valitse toiminto: ");

        // Luetaan käyttäjän valinta ja suoritetaan sitä vastaava toiminto.
        switch (Console.ReadLine()?.Trim())
        {
            case "1":
                await ListUsersAsync(connection);
                break;
            case "2":
                await AddUserAsync(connection);
                break;
            case "0":
                return;
            default:
                Console.WriteLine("Virheellinen valinta. Valitse 0, 1 tai 2.");
                break;
        }
    }
}
// Käsitellään SQL Serveriin liittyvät virheet SqlException-luokan avulla.
catch (SqlException exception)
{
    Console.Error.WriteLine("Tietokantatoiminto epäonnistui.");
    Console.Error.WriteLine(exception.Message);
    Environment.ExitCode = 1;
}

static async Task ListUsersAsync(SqlConnection connection)
{
    // Käyttämällä SQL-kyselyä haetaan kaikki käyttäjät taulusta dbo.Users.
    const string sql = """
        SELECT UserId, FirstName, LastName, Email, CreatedAt
        FROM dbo.Users
        ORDER BY UserId;
        """;

    // SqlCommand lähettää määritellyn SQL-kyselyn avoimeen tietokantayhteyteen.
    await using var command = new SqlCommand(sql, connection);
    await using var reader = await command.ExecuteReaderAsync();

    // Tulostetaan otsikot ja sarakkeiden nimet konsoliin.
    Console.WriteLine();
    Console.WriteLine("Käyttäjät");
    Console.WriteLine(new string('-', 100));
    Console.WriteLine($"{"Id",-6} {"Etunimi",-18} {"Sukunimi",-18} {"Sähköposti",-32} {"Luotu"}");
    Console.WriteLine(new string('-', 100));

    var usersFound = false;

    // Käydään while-loopissa läpi kaikki rivit, jotka reader palauttaa.
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

static async Task AddUserAsync(SqlConnection connection)
{
    Console.WriteLine();
    Console.WriteLine("Uuden käyttäjän lisääminen");

    // Pyydetään uuden käyttäjän tiedot konsolista.
    // Apumetodit varmistavat, että tiedot ovat kelvollisia ennen tallentamista.
    var firstName = ReadRequiredValue("Etunimi: ");
    var lastName = ReadRequiredValue("Sukunimi: ");
    var email = ReadEmailAddress();

    // Lisätään käyttäjä dbo.Users-tauluun parametrisoidulla SQL-kyselyllä.
    // Parametrien käyttäminen estää syötteiden tulkitsemisen osaksi SQL-komentoa.
    // OUTPUT palauttaa tietokannan uudelle käyttäjälle luoman UserId-tunnuksen.
    const string sql = """
        INSERT INTO dbo.Users (FirstName, LastName, Email, CreatedAt)
        OUTPUT INSERTED.UserId
        VALUES (@FirstName, @LastName, @Email, @CreatedAt);
        """;

    // Liitetään käyttäjän syöttämät arvot SQL-kyselyn parametreihin.
    await using var command = new SqlCommand(sql, connection);
    command.Parameters.AddWithValue("@FirstName", firstName);
    command.Parameters.AddWithValue("@LastName", lastName);
    command.Parameters.AddWithValue("@Email", email);
    command.Parameters.AddWithValue("@CreatedAt", DateTime.Now);

    // ExecuteScalarAsync suorittaa kyselyn ja palauttaa sen ensimmäisen arvon,
    // joka on tässä tapauksessa lisätyn käyttäjän UserId.
    var userId = (int)(await command.ExecuteScalarAsync()
        ?? throw new InvalidOperationException("Lisätyn käyttäjän tunnusta ei saatu tietokannasta."));

    Console.WriteLine($"Käyttäjä lisättiin onnistuneesti tunnuksella {userId}.");
}

static string ReadRequiredValue(string prompt)
{
    // Pyydetään arvoa uudelleen niin kauan, kunnes käyttäjä antaa muun kuin tyhjän arvon.
    while (true)
    {
        Console.Write(prompt);
        var value = Console.ReadLine()?.Trim();

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        Console.WriteLine("Arvo ei voi olla tyhjä.");
    }
}

static string ReadEmailAddress()
{
    // MailAddress.TryCreate tarkistaa, että annettu arvo on sähköpostiosoitteen muodossa.
    while (true)
    {
        var email = ReadRequiredValue("Sähköposti: ");

        if (MailAddress.TryCreate(email, out _))
        {
            return email;
        }

        Console.WriteLine("Anna kelvollinen sähköpostiosoite.");
    }
}
