using SampleApp;

var localization = new Localization();

Console.WriteLine(localization.R.UserStatus.online());

var b = R.Main.HelloWorld;

Console.WriteLine(localization.R[R.Main.Oro]);

var a = new PolishTextProvider();

localization.SetLanguage(SupportedLanguage.Polish);

Console.WriteLine(localization.R.Ooro.oro2);

var status = localization.R.UserStatus;

Console.WriteLine(localization.Language);
Console.WriteLine(status.online);
Console.WriteLine(status.doNotDisturb);
Console.WriteLine(status.away);

Console.WriteLine();

Console.WriteLine(localization.R.Main.HelloWorld);

Console.WriteLine();

localization.SetLanguage(SupportedLanguage.English);

Console.WriteLine(localization.Language);
Console.WriteLine(status.online);
Console.WriteLine(status.doNotDisturb);
Console.WriteLine(status.away);

Console.WriteLine();

Console.WriteLine(localization.R.Main.HelloWorld);

Console.WriteLine();

localization.SetLanguage(SupportedLanguage.German);

Console.WriteLine(localization.Language);
Console.WriteLine(status.online);
Console.WriteLine(status.doNotDisturb);
Console.WriteLine(status.away);

Console.WriteLine();

Console.WriteLine(localization.R.Main.HelloWorld);
