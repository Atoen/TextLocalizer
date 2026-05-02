using SampleApp;
using TextLocalizer.Types;

var localization = new Localization();

LocalizedTextProvider a = new PolishTextProvider();

// var itemsCount = localization.R.Main.ItemsCount;

foreach (var i in Enumerable.Sequence(0, 6, 1).Select(x =>
         {
             Console.WriteLine(localization.R.Main.ItemsCount(x));
             return 0;
         })) ;

localization.SetLanguage(SupportedLanguage.English);

foreach (var i in Enumerable.Sequence(0, 6, 1).Select(x =>
         {
             Console.WriteLine(localization.R.Main.ItemsCount(x));
             return 0;
         })) ;

localization.SetLanguage(SupportedLanguage.German);

foreach (var i in Enumerable.Sequence(0, 6, 1).Select(x =>
         {
             Console.WriteLine(localization.R.Main.ItemsCount(x));
             return 0;
         })) ;
    
