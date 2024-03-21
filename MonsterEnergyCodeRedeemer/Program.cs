using HtmlAgilityPack;
using MonsterEnergyCodeRedeemer.Classes;
using MonsterEnergyCodeRedeemer.Misc;
using MonsterEnergyCodeRedeemer.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;

#region Init
using HttpClient httpClient = new();
httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));
#endregion

#region User aus Datei lesen
UserModel? user = GetUserData();
if (user is null)
{
    Console.WriteLine("Fehler: User Daten konnten nicht gelesen werden!");
    Exit();
}
#endregion


bool loginSuccess = await Login();

if (!loginSuccess)
    Exit();

Exit();

#region Methods
async Task<bool> Login()
{
    string html = await httpClient.GetStringAsync(Endpoints.Login);

    string? csrfToken = GetCSRFToken(html);

    if (string.IsNullOrEmpty(csrfToken))
    {
        Console.WriteLine("Fehler: Keinen CSRF Token gefunden!");
        Exit();
    }

    await Console.Out.WriteLineAsync($"CSRF Token: {csrfToken}");

    string payLoad = $"email={user.Email}&password={user.Password}&_csrf_token={csrfToken}";

    HttpRequestMessage request = new(HttpMethod.Post, Endpoints.Login)
    {
        Content = new StringContent(payLoad, Encoding.UTF8, "application/x-www-form-urlencoded")
    };

    var response = await httpClient.SendAsync(request);

    if (!response.IsSuccessStatusCode)
    {
        await Console.Out.WriteLineAsync($"Fehler: Server antwortete mit Status {response.StatusCode} auf den Login!");
        return false;
    }

    string responseBody = await response.Content.ReadAsStringAsync();

    if (responseBody.Contains("E-Mail oder Passwort ist ungültig."))
    {
        await Console.Out.WriteLineAsync("Fehler: E-Mail oder Passwort ist ungültig!");
        return false;
    }

    await Console.Out.WriteLineAsync("Login erfolgreich");
    return true;
}

void Exit()
{
    Console.WriteLine("\n\nZum beenden eine Taste drücken.");
    Console.ReadKey();
    Environment.Exit(0);
}

string? GetCSRFToken(string html)
{
    XPathQueryBuilder? xpathBuilder = new XPathQueryBuilder().Query(html);

    HtmlNode? csrfTokenNode = xpathBuilder.ByAttributeValue("name", "_csrf_token").Result;

    if (csrfTokenNode is null)
        return default;

    return csrfTokenNode.Attributes["value"].Value;
}

UserModel? GetUserData()
{
    string userFilePath = Path.Combine(Directory.GetCurrentDirectory(), "user.json");

    try
    {
        string? userFileData = File.ReadAllText(userFilePath);

        return JsonSerializer.Deserialize<UserModel>(userFileData);
    }
    catch (Exception)
    {
        return default;
    }
}
#endregion