using HtmlAgilityPack;
using MonsterEnergyCodeRedeemer.Classes;
using MonsterEnergyCodeRedeemer.Misc;
using MonsterEnergyCodeRedeemer.Models;
using System.Net;
using System.Text;
using System.Text.Json;

#region Init
using HttpClient httpClient = new();
#endregion

#region User aus Datei lesen
UserModel? user = GetUserData();
if (user is null)
{
    await Console.Out.WriteLineAsync("Fehler: User Daten konnten nicht gelesen werden!");
    Exit();
}
#endregion

#region Login
bool loginSuccess = await Login();

if (!loginSuccess)
    Exit();

await CheckClawPoints();
#endregion

#region Codes einlösen
IEnumerable<string>? codes = GetCodes();

if (codes is null || !codes.Any())
{
    await Console.Out.WriteLineAsync("Fehler: Keine Codes in der Datei oder Datei nicht gefunden!");
    Exit();
}

await Console.Out.WriteLineAsync($"Anzahl gefundener Codes: {codes!.Count()}x\n\n");
await RedeemCodes(codes!);
#endregion

await Console.Out.WriteLineAsync("Fehler: Keine Codes in der Datei oder Datei nicht gefunden!");
Exit();

#region Methods
async Task<bool> Login()
{
    string html = await httpClient.GetStringAsync(Endpoints.Login);

    string? csrfToken = GetCSRFToken(html);

    if (string.IsNullOrEmpty(csrfToken))
    {
        await Console.Out.WriteLineAsync("Fehler: Keinen CSRF Token gefunden!");
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
        await Console.Out.WriteLineAsync($"Fehler: Server antwortete mit Status {response.StatusCode} auf den Login!\n");
        return false;
    }

    string responseBody = await response.Content.ReadAsStringAsync();

    if (responseBody.Contains("E-Mail oder Passwort ist ungültig."))
    {
        await Console.Out.WriteLineAsync("Fehler: E-Mail oder Passwort ist ungültig!\n");
        return false;
    }

    await Console.Out.WriteLineAsync("Login erfolgreich\n");
    return true;
}

async Task CheckClawPoints()
{
    string html = await httpClient.GetStringAsync(Endpoints.Dashboard);

    ClawPointsModel? clawPoints = GetClawPoints(html);

    if (clawPoints is null)
    {
        Console.Out.WriteLine("Fehler: Claw Points konnten nicht gelesen werden!");
    }
    else
    {
        await Console.Out.WriteLineAsync($"========\nClaw Points: Gesamt: {clawPoints.SumAll} | Summe: {clawPoints.Sum} | Eingelöst: {clawPoints.Claimed}\n========\n");
    }
}

ClawPointsModel? GetClawPoints(string html)
{
    XPathQueryBuilder? xPathBuilder = new XPathQueryBuilder().Query(html);

    List<HtmlNode>? claimedPointsNodes = xPathBuilder.ByClass("dashboard-box").ByElement("p").Results;

    if (claimedPointsNodes is null || claimedPointsNodes.Count < 3)
        return default;

    int.TryParse(claimedPointsNodes[0].InnerText, out int clawPointsSumAll);
    int.TryParse(claimedPointsNodes[1].InnerText, out int clawPointsSum);
    int.TryParse(claimedPointsNodes[2].InnerText, out int clawPointsClaimed);

    return new()
    {
        SumAll = clawPointsSumAll,
        Sum = clawPointsSum,
        Claimed = clawPointsClaimed
    };
}

void Exit()
{
    Console.Out.WriteLine("\n\nZum beenden eine Taste drücken.");
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

IEnumerable<string>? GetCodes()
{
    string codesFilePath = Path.Combine(Directory.GetCurrentDirectory(), "codes.txt");

    try
    {
        return [.. File.ReadAllLines(codesFilePath)];
    }
    catch (Exception)
    {
        return default;
    }
}

async Task RedeemCodes(IEnumerable<string> codes)
{
    string html = await httpClient.GetStringAsync(Endpoints.RedeemCode);

    string? codeToken = GetRedeemCodeToken(html);

    if (string.IsNullOrEmpty(codeToken))
    {
        await Console.Out.WriteLineAsync("Fehler: Redeem Code Token konnte nicht gelesen werden!");
        Exit();
    }

    foreach (string code in codes)
    {
        string? payLoad = GetRedeemCodePayload(code, codeToken);

        if (string.IsNullOrEmpty(payLoad))
        {
            await Console.Out.WriteLineAsync($"Fehler: Code Länge falsch! Code: {code}");
            Exit();
        }

        HttpRequestMessage request = new(HttpMethod.Post, Endpoints.RedeemCode)
        {
            Content = new StringContent(payLoad, Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        HttpResponseMessage? response = await httpClient.SendAsync(request);

        string responseBody = await response.Content.ReadAsStringAsync();

        if (!HasRedeemError(responseBody, code))
        {
            await Console.Out.WriteLineAsync($"\nErfolgreich:\nCode: {code} eingelöst!");
            await Wait(1);
            await CheckClawPoints();
            await Wait(15);
        }
        else
        {
            await Wait(3);
        }
    }

}

string? GetRedeemCodePayload(string code, string codeToken)
{
    if (code.Length == 10)
    {
        return $"code[codeTen][codeTen1]={code[0]}" +
               $"&code[codeTen][codeTen2]={code[1]}" +
               $"&code[codeTen][codeTen3]={code[2]}" +
               $"&code[codeTen][codeTen4]={code[3]}" +
               $"&code[codeTen][codeTen5]={code[4]}" +
               $"&code[codeTen][codeTen6]={code[5]}" +
               $"&code[codeTen][codeTen7]={code[6]}" +
               $"&code[codeTen][codeTen8]={code[7]}" +
               $"&code[codeTen][codeTen9]={code[8]}" +
               $"&code[codeTen][codeTen10]={code[9]}" +
               $"&code[codeTwelve][codeTwelve1]=" +
               $"&code[codeTwelve][codeTwelve2]=" +
               $"&code[codeTwelve][codeTwelve3]=" +
               $"&code[codeTwelve][codeTwelve4]=" +
               $"&code[codeTwelve][codeTwelve5]=" +
               $"&code[codeTwelve][codeTwelve6]=" +
               $"&code[codeTwelve][codeTwelve7]=" +
               $"&code[codeTwelve][codeTwelve8]=" +
               $"&code[codeTwelve][codeTwelve9]=" +
               $"&code[codeTwelve][codeTwelve10]=" +
               $"&code[codeTwelve][codeTwelve11]=" +
               $"&code[codeTwelve][codeTwelve12]=" +
               $"&code[_token]={codeToken}";
    }
    else if (code.Length == 12)
    {
        return $"code[codeTen][codeTen1]=" +
               $"&code[codeTen][codeTen2]=" +
               $"&code[codeTen][codeTen3]=" +
               $"&code[codeTen][codeTen4]=" +
               $"&code[codeTen][codeTen5]=" +
               $"&code[codeTen][codeTen6]=" +
               $"&code[codeTen][codeTen7]=" +
               $"&code[codeTen][codeTen8]=" +
               $"&code[codeTen][codeTen9]=" +
               $"&code[codeTen][codeTen10]=" +
               $"&code[codeTwelve][codeTwelve1]={code[0]}" +
               $"&code[codeTwelve][codeTwelve2]={code[1]}" +
               $"&code[codeTwelve][codeTwelve3]={code[2]}" +
               $"&code[codeTwelve][codeTwelve4]={code[3]}" +
               $"&code[codeTwelve][codeTwelve5]={code[4]}" +
               $"&code[codeTwelve][codeTwelve6]={code[5]}" +
               $"&code[codeTwelve][codeTwelve7]={code[6]}" +
               $"&code[codeTwelve][codeTwelve8]={code[7]}" +
               $"&code[codeTwelve][codeTwelve9]={code[8]}" +
               $"&code[codeTwelve][codeTwelve10]={code[9]}" +
               $"&code[codeTwelve][codeTwelve11]={code[10]}" +
               $"&code[codeTwelve][codeTwelve12]={code[11]}" +
               $"&code[_token]={codeToken}";
    }

    return default;
}

string? GetRedeemCodeToken(string html)
{
    XPathQueryBuilder? xPathBuilder = new XPathQueryBuilder().Query(html);

    HtmlNode? codeTokenNode = xPathBuilder.ById("code__token").Result;

    if (codeTokenNode is null)
        return default;

    return codeTokenNode.Attributes["value"].Value;
}

bool HasRedeemError(string html, string code)
{
    XPathQueryBuilder? xPathBuilder = new XPathQueryBuilder().Query(html);

    HtmlNode? errorNode = xPathBuilder.ByClass("form-error-message").Result;

    if (errorNode is null)
        return false;

    Console.Out.WriteLine($"Fehler bei Übermittlung von Code: {code} => {errorNode.InnerText}");

    return true;
}

async Task Wait(int seconds)
{
    await Console.Out.WriteLineAsync($"Warte: {seconds} Sekunde(n)");
    await Task.Delay(TimeSpan.FromSeconds(seconds));
}
#endregion