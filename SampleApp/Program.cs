using SampleApp;

var localization = new Localization();

var main = localization.R.Main;

localization.SetLanguage(SupportedLanguage.Polish);

PrintHeader(localization.Language.ToString());
PrintCommon();
PrintTemplated();

Console.WriteLine();

localization.SetLanguage(SupportedLanguage.English);

PrintHeader(localization.Language.ToString());
PrintPresence();
PrintTemplated();

Console.WriteLine();

localization.SetLanguage(SupportedLanguage.German);

PrintHeader(localization.Language.ToString());
PrintPresence();
PrintTemplated();

Console.WriteLine();

PrintHeader("Switch stress test");

foreach (var lang in new[]
{
    SupportedLanguage.English,
    SupportedLanguage.Polish,
    SupportedLanguage.German,
    SupportedLanguage.English
})
{
    localization.SetLanguage(lang);
    Console.WriteLine($"[{localization.Language}] {main.WelcomeUser("Tester")}");
}

Console.WriteLine();

PrintHeader("Edge cases");

localization.SetLanguage(SupportedLanguage.English);

Console.WriteLine(main.WelcomeUser(""));
Console.WriteLine(main.ItemsCount(-1));
Console.WriteLine(main.ItemsCount(1000000));

Console.WriteLine();

PrintHeader("Cross-language consistency");

var testName = "Chris";
var testCount = 3;

foreach (var lang in new[]
{
    SupportedLanguage.English,
    SupportedLanguage.Polish,
    SupportedLanguage.German
})
{
    localization.SetLanguage(lang);

    Console.WriteLine(localization.Language);
    Console.WriteLine(main.WelcomeUser(testName));
    Console.WriteLine(main.ItemsCount(testCount));
    Console.WriteLine();
}

return;

void PrintHeader(string title)
{
    Console.WriteLine($"=== {title} ===");
}

void PrintCommon()
{
    Console.WriteLine(main.About);
    Console.WriteLine(main.AddFriend);
    Console.WriteLine(main.Back);
}

void PrintPresence()
{
    Console.WriteLine(main.Goodbye);
    Console.WriteLine(main.DarkMode);
    Console.WriteLine(main.Language);
}

void PrintTemplated()
{
    Console.WriteLine(main.WelcomeUser("Alice"));
    Console.WriteLine(main.WelcomeUser("Bob"));
    Console.WriteLine(main.ItemsCount(0));
    Console.WriteLine(main.ItemsCount(5));
    Console.WriteLine(main.ItemsCount(42));
}
