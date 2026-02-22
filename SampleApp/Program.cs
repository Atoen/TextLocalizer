using SampleApp;

var localization = new Localization();

var a = localization.R.Main.HelloWorld;

var b = localization.R.UserStatus;

var c = b.ToString();

// var a = R5.evening;
//
// //var farewellId = R2.farewell;
// //Console.WriteLine(localization.R[farewellId]);
//
// //var message = localization.StringResource(R2.templated, true);
// //Console.WriteLine(message);
//
// Console.WriteLine(localization.R.greetings);
//
// localization.SetLanguage(SupportedLanguage.Polish);
// Console.WriteLine(localization.R.greetings);
//
// localization.SetLanguage(SupportedLanguage.German);
// Console.WriteLine(localization.R.evening);
// Console.WriteLine(localization.R.untranslated_key);
